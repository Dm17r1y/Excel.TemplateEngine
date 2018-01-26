﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using SKBKontur.Catalogue.ExcelObjectPrinter.TableParser;

namespace SKBKontur.Catalogue.ExcelObjectPrinter.ParseCollection.Parsers
{
    public class EnumerableParser : IEnumerableParser
    {
        public EnumerableParser(IParserCollection parserCollection)
        {
            this.parserCollection = parserCollection;
        }

        [NotNull]
        public List<object> Parse([NotNull] ITableParser tableParser, [NotNull] Type modelType, int count, [NotNull] Action<string, string> addFieldMapping)
        {
            if (count > maxEnumerableLength)
                throw new NotSupportedException($"Lists longer than {maxEnumerableLength} are not supported");

            var parser = parserCollection.GetAtomicValueParser(modelType);
            var result = new List<object>();
            for (var i = 0; (count == -1 || i < count) && i < maxEnumerableLength; i++)
            {
                if (i != 0)
                    tableParser.MoveToNextLayer();

                tableParser.PushState();

                if(!parser.TryParse(tableParser, modelType, out var item) || item == null)
                {
                    if(count == -1)
                        break;
                    item = default;
                    // todo (mpivko, 29.01.2018): think carefully
                }

                addFieldMapping($"[{i}]", tableParser.CurrentState.Cursor.CellReference);
                result.Add(item);
                tableParser.PopState();
            }
            
            return result;
        }

        private readonly IParserCollection parserCollection;
        private const int maxEnumerableLength = (int)1e4;
    }
}