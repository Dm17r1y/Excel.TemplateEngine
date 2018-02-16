﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

using SKBKontur.Catalogue.ExcelObjectPrinter.Exceptions;
using SKBKontur.Catalogue.ExcelObjectPrinter.NavigationPrimitives;

namespace SKBKontur.Catalogue.ExcelObjectPrinter.Helpers
{
    public sealed class TemplateDescriptionHelper
    {
        private TemplateDescriptionHelper()
        {
        }

        public string GetTemplateNameFromValueDescription(string expression)
        {
            return !IsCorrectValueDescription(expression) ? null : GetDescriptionParts(expression)[1];
        }

        public string GetFormControlNameFromValueDescription(string expression)
        {
            return !IsCorrectFormValueDescription(expression) ? null : GetDescriptionParts(expression)[1];
        }

        public string GetFormControlTypeFromValueDescription(string expression)
        {
            return !IsCorrectFormValueDescription(expression) ? null : GetDescriptionParts(expression)[0];
        }

        public bool IsCorrectValueDescription(string expression)
        {
            var descriptionParts = GetDescriptionParts(expression);
            return IsCorrectAbstractValueDescription(expression) && descriptionParts[0] == "Value";
        }

        public bool IsCorrectFormValueDescription(string expression)
        {
            var descriptionParts = GetDescriptionParts(expression);
            return IsCorrectAbstractValueDescription(expression) && !string.IsNullOrEmpty(descriptionParts[1]) && formControlTypes.Contains(descriptionParts[0]);
        }

        public bool IsCorrectAbstractValueDescription(string expression)
        {
            var descriptionParts = GetDescriptionParts(expression);
            if(descriptionParts.Count() != 3 ||
               string.IsNullOrEmpty(descriptionParts[2]))
                return false;

            return IsCorrectModelPath(descriptionParts[2]);
        }

        public bool IsCorrectModelPath(string pathParts)
        {
            return pathRegex.IsMatch(pathParts);
        }

        public bool IsCorrectTemplateDescription(string expression)
        {
            var descriptionParts = GetDescriptionParts(expression);
            if(descriptionParts.Count() != 4 ||
               descriptionParts[0] != "Template" ||
               string.IsNullOrEmpty(descriptionParts[1]))
                return false;

            return exactCellReferenceRegex.IsMatch(descriptionParts[2]) &&
                   exactCellReferenceRegex.IsMatch(descriptionParts[3]);
        }

        public bool TryExtractCoordinates(string templateDescription, out IRectangle rectangle)
        {
            rectangle = null;
            if(!IsCorrectTemplateDescription(templateDescription))
                return false;

            rectangle = ExctractCoordinates(templateDescription);
            return true;
        }

        private static IRectangle ExctractCoordinates(string expression)
        {
            var upperLeft = new CellPosition(cellReferenceRegex.Matches(expression)[0].Value);
            var lowerRight = new CellPosition(cellReferenceRegex.Matches(expression)[1].Value);
            return new Rectangle(upperLeft, lowerRight);
        }

        public string[] GetDescriptionParts(string filedDescription)
        {
            return string.IsNullOrEmpty(filedDescription) ? new string[0] : filedDescription.Split(':').ToArray();
        }

        public string GetPathPartName(string pathPart)
        {
            if(IsArrayPathPart(pathPart))
                return pathPart.Replace("[]", "");
            if(IsCollectionAccessPathPart(pathPart))
                return GetCollectionAccessPathPartName(pathPart);
            return pathPart;
        }

        public bool IsArrayPathPart(string pathPart)
        {
            return arrayPathPartRegex.IsMatch(pathPart);
        }

        public string GetCollectionAccessPathPartName(string pathPart)
        {
            return GetCollectionAccessPathPart(pathPart).name;
        }

        public string GetCollectionAccessPathPartIndex(string pathPart)
        {
            return GetCollectionAccessPathPart(pathPart).index;
        }

        public bool IsCollectionAccessPathPart(string pathPart)
        {
            return collectionAccessPathPartRegex.IsMatch(pathPart);
        }

        private (string name, string index) GetCollectionAccessPathPart(string pathPart)
        {
            var match = collectionAccessPathPartRegex.Match(pathPart);
            if(!match.Success)
                throw new ArgumentException($"{nameof(pathPart)} should be collection access path part");
            return (match.Groups[1].Value, match.Groups[2].Value);
        }

        [NotNull]
        public static object ParseCollectionIndexer([NotNull] string collectionIndexer, [NotNull] Type collectionKeyType)
        {
            if (collectionIndexer.StartsWith("\"") && collectionIndexer.EndsWith("\""))
            {
                if (collectionKeyType != typeof(string))
                    throw new ObjectPropertyExtractionException($"Collection with '{collectionKeyType}' keys was indexed by {typeof(string)}");
                return collectionIndexer.Substring(1, collectionIndexer.Length - 2);
            }
            if (int.TryParse(collectionIndexer, out var intIndexer))
            {
                if (collectionKeyType != typeof(int))
                    throw new ObjectPropertyExtractionException($"Collection with '{collectionKeyType}' keys was indexed by {typeof(int)}");
                return intIndexer;
            }
            throw new ObjectPropertyExtractionException("Only strings and ints are supported as collection indexers");
        }

        public static TemplateDescriptionHelper Instance { get; } = new TemplateDescriptionHelper();

        private static readonly Regex collectionAccessPathPartRegex = new Regex(@"^(\w+)\[([^\[\]]+)\]$", RegexOptions.Compiled);
        private static readonly Regex arrayPathPartRegex = new Regex(@"^(\w+)\[\]$", RegexOptions.Compiled);
        private static readonly Regex pathRegex = new Regex(@"^[A-Za-z]\w*(\[[^\[\]]*\])?(\.[A-Za-z]\w*(\[[^\[\]]*\])?)*$", RegexOptions.Compiled);
        private static readonly Regex cellReferenceRegex = new Regex("[A-Z]+[1-9][0-9]*", RegexOptions.Compiled);
        private static readonly Regex exactCellReferenceRegex = new Regex("^[A-Z]+[1-9][0-9]*$", RegexOptions.Compiled);
        private readonly HashSet<string> formControlTypes = new HashSet<string>(new[] {"CheckBox", "DropDown"});
    }
}