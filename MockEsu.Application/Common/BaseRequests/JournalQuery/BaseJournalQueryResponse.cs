﻿using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Extensions.JournalFilters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MockEsu.Application.Common.BaseRequests.JournalQuery
{
    public abstract class BaseJournalQueryResponse<TResult> : BaseResponse where TResult : BaseDto
    {
        public abstract IList<TResult> Journal { get; set; }
    }
}
