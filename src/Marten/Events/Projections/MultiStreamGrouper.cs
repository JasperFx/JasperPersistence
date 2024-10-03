﻿using System;
using System.Collections.Generic;
using Marten.Events.Aggregation;

namespace Marten.Events.Projections;

/// <summary>
///     This type of grouper potentially sorts one event into multiple aggregates
/// </summary>
/// <typeparam name="TId"></typeparam>
/// <typeparam name="TEvent"></typeparam>
internal class MultiStreamGrouper<TId, TEvent>: IGrouper<TId>
{
    private readonly Func<TEvent, IReadOnlyList<TId>> _func;

    public MultiStreamGrouper(Func<TEvent, IReadOnlyList<TId>> expression)
    {
        _func = expression;
    }

    public void Apply(IEnumerable<IEvent> events, ITenantSliceGroup<TId> grouping)
    {
        grouping.AddEvents(_func, events);
    }
}

/// <summary>
///     This type of grouper potentially sorts one event into multiple aggregates
/// </summary>
/// <typeparam name="TId"></typeparam>
/// <typeparam name="TEvent"></typeparam>
internal class MultiStreamGrouperWithMetadata<TId, TEvent>: IGrouper<TId>
{
    private readonly Func<IEvent<TEvent>, IReadOnlyList<TId>> _func;

    public MultiStreamGrouperWithMetadata(Func<IEvent<TEvent>, IReadOnlyList<TId>> expression)
    {
        _func = expression;
    }

    public void Apply(IEnumerable<IEvent> events, ITenantSliceGroup<TId> grouping)
    {
        grouping.AddEventsWithMetadata(_func, events);
    }
}

