﻿// ==========================================================================
//  MongoEventCommit.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Squidex.Infrastructure.MongoDb.EventStore
{
    public sealed class MongoEventCommit
    {
        [BsonId]
        [BsonElement]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        [BsonRequired]
        [BsonElement]
        public DateTime Timestamp { get; set; }

        [BsonElement]
        [BsonRequired]
        public List<MongoEvent> Events { get; set; }

        [BsonElement]
        [BsonRequired]
        public string EventStream { get; set; }

        [BsonElement]
        [BsonRequired]
        public int EventsVersion { get; set; }

        [BsonElement]
        [BsonRequired]
        public int EventCount { get; set; }
    }
}