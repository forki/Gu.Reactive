﻿namespace Gu.Reactive
{
    using System.Collections.Generic;
    using System.Linq;

    internal class OrConditionCollection : ConditionCollection
    {
        public OrConditionCollection(params ICondition[] conditions)
            : base(GetIsSatisfied, conditions)
        {
        }

        private static bool? GetIsSatisfied(IReadOnlyList<ICondition> conditions)
        {
            if (conditions.Any(x => x.IsSatisfied == true))
            {
                return true;
            }

            if (conditions.All(x => x.IsSatisfied == false))
            {
                return false;
            }

            return null; // Mix of false & nulls means not enough info
        }
    }
}