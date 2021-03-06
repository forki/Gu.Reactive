﻿namespace Gu.Reactive
{
    using System;
    using System.Linq;

    /// <summary>
    /// Helpers for <see cref="Type"/>
    /// </summary>
    public static class TypeExt
    {
        /// <summary>
        /// Returns nicely formatted type names for generic types.
        /// </summary>
        public static string PrettyName(this Type type)
        {
            if (type == typeof(long))
            {
                return "long";
            }

            if (type == typeof(double))
            {
                return "double";
            }

            if (type == typeof(int))
            {
                return "int";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type.IsGenericType)
            {
                var arguments = string.Join(", ", type.GenericTypeArguments.Select(PrettyName));
                return $"{type.Name.Split('`').First()}<{arguments}>";
            }

            return type.Name;
        }

        /// <summary>
        /// Check if a type is Nullable`1
        /// </summary>
        public static bool IsNullable(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}
