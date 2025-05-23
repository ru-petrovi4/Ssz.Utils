﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CodeGenerator.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides functionality to generate C# code for the specified <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable

namespace OxyPlot
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides functionality to generate C# code for the specified <see cref="PlotModel" />.
    /// </summary>
    /// <remarks>This is useful for creating examples or unit tests. Press Ctrl+Alt+C in a plot to copy code to the clipboard.
    /// Usage:
    /// <code>
    /// var cg = new CodeGenerator(myPlotModel);
    /// Clipboard.SetText(cg.ToCode());
    /// </code></remarks>
    public class CodeGenerator
    {
        /// <summary>
        /// The string builder.
        /// </summary>
        private readonly StringBuilder sb;

        /// <summary>
        /// The variables.
        /// </summary>
        private readonly Dictionary<string, bool> variables;

        /// <summary>
        /// The indent string.
        /// </summary>
        private string indentString = string.Empty;

        /// <summary>
        /// The current number of indents.
        /// </summary>
        private int indents;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerator" /> class.
        /// </summary>
        /// <param name="model">The model.</param>
        public CodeGenerator(PlotModel model)
        {
            this.variables = new Dictionary<string, bool>();
            this.sb = new StringBuilder();
            this.Indents = 8;
            var title = model.Title ?? "Untitled";
            this.AppendLine("[Example({0})]", title.ToCode());
            string methodName = this.MakeValidVariableName(title);
            this.AppendLine("public static PlotModel {0}()", methodName);
            this.AppendLine("{");
            this.Indents += 4;
            string modelName = this.Add(model);
            this.AddChildren(modelName, "Axes", model.Axes);
            this.AddChildren(modelName, "Series", model.Series);
            this.AddChildren(modelName, "Annotations", model.Annotations);
            this.AddChildren(modelName, "Legends", model.Legends);
            this.AppendLine("return {0};", modelName);
            this.Indents -= 4;
            this.AppendLine("}");
        }

        /// <summary>
        /// Gets or sets the number of indents.
        /// </summary>
        private int Indents
        {
            get
            {
                return this.indents;
            }

            set
            {
                this.indents = value;
                this.indentString = new string(' ', value);
            }
        }

        /// <summary>
        /// Formats the code.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="values">The values.</param>
        /// <returns>The format code.</returns>
        public static string FormatCode(string format, params object[] values)
        {
            var encodedValues = new object?[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                encodedValues[i] = values[i].ToCode();
            }

            return string.Format(format, encodedValues);
        }

        /// <summary>
        /// Formats a constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="format">The format of the constructor arguments.</param>
        /// <param name="values">The argument values.</param>
        /// <returns>The format constructor.</returns>
        public static string FormatConstructor(Type type, string format, params object[] values)
        {
            return string.Format("new {0}({1})", type.Name, FormatCode(format, values));
        }

        /// <summary>
        /// Returns the c# code for this model.
        /// </summary>
        /// <returns>C# code.</returns>
        public string ToCode()
        {
            return this.sb.ToString();
        }

        /// <summary>
        /// Adds the specified object to the generated code.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The variable name.</returns>
        private string Add(object obj)
        {
            var type = obj.GetType();

            var hasParameterLessCtor = type.GetTypeInfo().DeclaredConstructors.Any(ci => ci.GetParameters().Length == 0);

            if (!hasParameterLessCtor)
            {
                return string.Format("/* Cannot generate code for {0} constructor */", type.Name);
            }

            var defaultInstance = Activator.CreateInstance(type)!;
            var varName = this.GetNewVariableName(type);
            this.variables.Add(varName, true);
            this.AppendLine("var {0} = new {1}();", varName, type.Name);
            this.SetProperties(obj, varName, defaultInstance);
            return varName;
        }

        /// <summary>
        /// Adds the children.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="children">The children.</param>
        private void AddChildren(string name, string collectionName, IEnumerable children)
        {
            foreach (var child in children)
            {
                string childName = this.Add(child);
                this.AppendLine("{0}.{1}.Add({2});", name, collectionName, childName);
            }
        }

        /// <summary>
        /// Adds the items.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="list">The list.</param>
        private void AddItems(string name, IList list)
        {
            foreach (var item in list)
            {
                var code = item.ToCode();
                if (code == null)
                {
                    continue;
                }

                this.AppendLine("{0}.Add({1});", name, code);
            }
        }

        /// <summary>
        /// Creates and sets the elements of an array.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="array">The array.</param>
        private void AddArray(string name, Array array)
        {
            var elementType = array.GetType().GetElementType()!;
            if (array.Rank == 1)
            {
                this.AppendLine("{0} = new {1}[{2}];", name, elementType.Name, array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    var code = array.GetValue(i).ToCode();
                    if (code == null)
                    {
                        continue;
                    }

                    this.AppendLine("{0}[{1}] = {2};", name, i, code);
                }
            }

            if (array.Rank == 2)
            {
                this.AppendLine("{0} = new {1}[{2}, {3}];", name, elementType.Name, array.GetLength(0), array.GetLength(1));
                for (int i = 0; i < array.GetLength(0); i++)
                {
                    for (int j = 0; j < array.GetLength(1); j++)
                    {
                        var code = array.GetValue(i, j).ToCode();
                        if (code == null)
                        {
                            continue;
                        }

                        this.AppendLine("{0}[{1}, {2}] = {3};", name, i, j, code);
                    }
                }
            }

            if (array.Rank > 2)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Appends the line.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The args.</param>
        private void AppendLine(string format, params object[] args)
        {
            if (args.Length > 0)
            {
                this.sb.AppendLine(this.indentString + string.Format(CultureInfo.InvariantCulture, format, args));
            }
            else
            {
                this.sb.AppendLine(this.indentString + format);
            }
        }

        /// <summary>
        /// Determines if the two specified lists are equal.
        /// </summary>
        /// <param name="list1">The first list.</param>
        /// <param name="list2">The second list.</param>
        /// <returns>True if all items are equal.</returns>
        private bool AreListsEqual(IList? list1, IList? list2)
        {
            if (list1 == null || list2 == null)
            {
                return false;
            }

            if (list1.Count != list2.Count)
            {
                return false;
            }

            for (int i = 0; i < list1.Count; i++)
            {
                if (!list1[i]!.Equals(list2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get the first attribute of the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="pi">The property info.</param>
        /// <returns>The attribute, or <c>null</c> if no attribute was found.</returns>
        private T? GetFirstAttribute<T>(PropertyInfo pi) where T : Attribute
        {
            return pi.GetCustomAttributes(typeof(CodeGenerationAttribute), true).OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets a new variable name of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The variable name.</returns>
        private string GetNewVariableName(Type type)
        {
            string prefix = type.Name;
            prefix = char.ToLower(prefix[0]) + prefix.Substring(1);
            int i = 1;
            while (this.variables.ContainsKey(prefix + i))
            {
                i++;
            }

            return prefix + i;
        }

        /// <summary>
        /// Makes a valid variable name of a string. Invalid characters will simply be removed.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <returns>A valid variable name.</returns>
        private string MakeValidVariableName(string title)
        {
            title = title ?? throw new ArgumentNullException(nameof(title));

            var regex = new Regex("[a-zA-Z_][a-zA-Z0-9_]*");
            var result = new StringBuilder();
            foreach (var c in title)
            {
                string s = c.ToString();
                if (regex.Match(s).Success)
                {
                    result.Append(s);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// The set properties.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="varName">The variable name.</param>
        /// <param name="defaultValues">The default values.</param>
        private void SetProperties(object instance, string varName, object defaultValues)
        {
            var instanceType = instance.GetType();
            var listsToAdd = new Dictionary<string, IList>();
            var arraysToAdd = new Dictionary<string, Array>();

            var properties = instanceType.GetRuntimeProperties().Where(pi => pi.GetMethod!.IsPublic && !pi.GetMethod.IsStatic);

            foreach (var pi in properties)
            {
                // check the [CodeGeneration] attribute
                var cga = this.GetFirstAttribute<CodeGenerationAttribute>(pi);
                if (cga != null && !cga.GenerateCode)
                {
                    continue;
                }

                string name = varName + "." + pi.Name;
                object? value = pi.GetValue(instance, null);
                object? defaultValue = pi.GetValue(defaultValues, null);

                // check if lists are equal
                if (this.AreListsEqual(value as IList, defaultValue as IList))
                {
                    continue;
                }

                var array = value as Array;
                if (array != null)
                {
                    arraysToAdd.Add(name, array);
                    continue;
                }

                // add items of lists
                var list = value as IList;
                if (list != null)
                {
                    listsToAdd.Add(name, list);
                    continue;
                }

                // only properties with public setters are used
                var setter = pi.SetMethod;
                if (setter == null || !setter.IsPublic)
                {
                    continue;
                }

                // skip default values
                if ((value != null && value.Equals(defaultValue)) || value == defaultValue)
                {
                    continue;
                }

                this.SetProperty(name, value);
            }

            // Add the items of the lists
            foreach (var kvp in listsToAdd)
            {
                var name = kvp.Key;
                var list = kvp.Value;
                this.AddItems(name, list);
            }

            foreach (var kvp in arraysToAdd)
            {
                var name = kvp.Key;
                var array = kvp.Value;
                this.AddArray(name, array);
            }
        }

        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value.</param>
        private void SetProperty(string name, object? value)
        {
            string? code = value.ToCode();
            if (code != null)
            {
                this.AppendLine("{0} = {1};", name, code);
            }
        }
    }
}
