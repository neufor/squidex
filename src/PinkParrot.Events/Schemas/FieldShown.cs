﻿// ==========================================================================
//  FieldShown.cs
//  PinkParrot Headless CMS
// ==========================================================================
//  Copyright (c) PinkParrot Group
//  All rights reserved.
// ==========================================================================

using PinkParrot.Infrastructure;
using PinkParrot.Infrastructure.CQRS.Events;

namespace PinkParrot.Events.Schemas
{
    [TypeName("FieldShownEvent")]
    public class FieldShown : IEvent
    {
        public long FieldId { get; set; }
    }
}