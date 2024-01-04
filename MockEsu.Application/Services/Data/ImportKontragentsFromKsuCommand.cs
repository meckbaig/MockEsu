using MediatR;
using Microsoft.AspNetCore.Http;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System;

namespace MockEsu.Application.Services.Data;

public record ImportKontragentsFromKsuCommand : BaseRequest<ImportKontragentsFromKsuResponse>
{
    public IFormFile file { get; set; }
}

public class ImportKontragentsFromKsuResponse : BaseResponse
{
    public bool result { get; set; }
}

public class ImportKontragentsFromKsuCommandHandler : IRequestHandler<ImportKontragentsFromKsuCommand, ImportKontragentsFromKsuResponse>
{
    private readonly IAppDbContext _context;

    public ImportKontragentsFromKsuCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ImportKontragentsFromKsuResponse> Handle(ImportKontragentsFromKsuCommand request, CancellationToken cancellationToken)
    {
        bool result = false;
        dynamic json = ParseFile(request.file);
        dynamic kontragentsJson = json.kontragent;
        int count = 0;
        foreach (dynamic k in kontragentsJson)
        {
            PaymentContract contract = AddContract(k);
            Address address = AddAddress(k);
            string phone = k.phone ?? "";
            Regex rgx = new Regex("[^0-9]");
            if (phone != null)
                phone = rgx.Replace(phone, "");
            else
                phone = "";
            string name = k.full_name;
            if (name == null)
                name = "Пустое имя";
            Kontragent newKontragent = new Kontragent() 
            {
                Name = name,
                PhoneNumber = phone,
                AddressId = address.Id,
            };
            Kontragent kontragent = _context.Kontragents.FirstOrDefault(k => k.Name == newKontragent.Name && k.PhoneNumber == newKontragent.PhoneNumber);
            if (kontragent == null)
            {
                _context.Kontragents.Add(newKontragent);
                _context.SaveChanges();
                kontragent = newKontragent;
            }
            if (kontragent != null)
            {
                kontragent.AddressId = address.Id;
                _context.SaveChanges();
                AddKontragentAgreement(k, kontragent, contract);
            }
            Console.WriteLine($"{++count}/15000");
        }
        if (_context.Kontragents.Count() == 15000)
            result = true;
        return new ImportKontragentsFromKsuResponse() { result = result };
    }

    private dynamic ParseFile(IFormFile file)
    {
        var jsonSB = new StringBuilder();
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            while (reader.Peek() >= 0)
                jsonSB.AppendLine(reader.ReadLine());
        }
        return JsonConvert.DeserializeObject<dynamic>(jsonSB.ToString());
    }

    private KontragentAgreement AddKontragentAgreement(dynamic k, Kontragent kontragent, PaymentContract contract)
    {
        KontragentAgreement agreement = _context.KontragentAgreements.FirstOrDefault(a => a.KontragentId == kontragent.Id);
        if (agreement != null)
            return agreement;
        string dateString = k.date_contract;
        DateOnly? dateOnly = null;
        try
        {
            DateTimeOffset dtoff = k.date_contract;
            dateOnly = DateOnly.FromDateTime(dtoff.Date);
        }
        catch (Exception)
        {
            //string[] date = ((string)k.date_contract).Split('.');
            //dateOnly = new DateOnly(Convert.ToInt32(date[2]), Convert.ToInt32(date[1]), Convert.ToInt32(date[0]));
        }
        agreement = new KontragentAgreement()
        {
            DocumentNumber = k.contract,
            PersonalAccount = k.pa,
            ContractDate = dateOnly,
            Balance = k.balance,
            OrganizationId = Convert.ToInt32(k.org),
            Kontragent = kontragent,
            PaymentContract = contract,
        };
        _context.KontragentAgreements.Add(agreement);
        _context.SaveChanges();
        return agreement;
    }

    private Organization AddOrganization()
    {
        throw new NotImplementedException();
    }

    private Address AddAddress(dynamic k)
    {
        if (k?.address != null)
        {
            string addressString = k.address[1];
            string[] addressSplit = addressString.Split(',');
            City city = AddCity(addressSplit[0]);
            Street street = AddStreet(addressSplit[1], city);
            string areaName = string.Empty;
            try
            {
                
                areaName = k?.area_model?.name;
            }
            catch (Exception)
            {
            }
            Region region = AddRegion(areaName);
            Address newAddress;
            try
            {
                newAddress = new Address()
                {
                    City = city,
                    Street = street,
                    HouseName = addressSplit[2].Split('.')[1].Trim(),
                    PorchNumber = Convert.ToInt32(addressSplit[3].Split('.')[1].Trim()),
                    Apartment = addressSplit[4].Split('.')[1].Trim(),
                    Region = region,
                };
            }
            catch (Exception)
            {

                throw;
            }
            Address address = _context.Addresses.FirstOrDefault(a => a.City == newAddress.City
                                                                  && a.Street == newAddress.Street
                                                                  && a.HouseName == newAddress.HouseName
                                                                  && a.PorchNumber == newAddress.PorchNumber
                                                                  && a.Apartment == newAddress.Apartment
                                                                  && a.Region == newAddress.Region);
            if (address != null)
                return address;
            _context.Addresses.Add(newAddress);
            _context.SaveChanges();
            return newAddress;
        }
        return null;
    }

    private Region AddRegion(string name)
    {
        Region region = _context.Regions.FirstOrDefault(r => r.Name == name);
        if (region != null)
            return region;
        region = new Region() { Name = name };
        _context.Regions.Add(region);
        _context.SaveChanges();
        return region;
    }

    private City AddCity(string citystr)
    {
        string cityName = citystr.Split('.')[1].Trim();
        City city = _context.Cities.FirstOrDefault(c => c.Name == cityName);
        if (city != null)
            return city;
        city = new City() { Name = cityName };
        _context.Cities.Add(city);
        _context.SaveChanges();
        return city;
    }

    private Street AddStreet(string streetstr, City city)
    {
        string streetName = streetstr.Split('.')[1].Trim();
        Street street = _context.Streets.FirstOrDefault(s => s.Name == streetName);
        if (street != null) 
            return street;
        street = new Street() { Name = streetName, City = city };
        _context.Streets.Add(street);
        _context.SaveChanges();
        return street;
    }

    private PaymentContract AddContract(dynamic k)
    {
        if (k?.tmpl_contract_model != null)
        {
            string contractName = k?.tmpl_contract_model?.name;
            PaymentContract existingContract = _context.PaymentContracts.FirstOrDefault(pc => pc.Name == contractName);
            if (existingContract != null)
                return existingContract;
            existingContract = new PaymentContract
            {
                //Id = k.tmpl_contract_model.id,
                Name = k.tmpl_contract_model.name,
                Day = k?.tmpl_contract_model.day,
                Rent = k?.tmpl_contract_model.rent,
                Frequency = k?.tmpl_contract_model.freq,
                Print = k?.tmpl_contract_model.print,
            };
            _context.PaymentContracts.Add(existingContract);
            _context.SaveChanges();
            return existingContract;
        }
        return null;
    }
}
