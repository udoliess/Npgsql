﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Npgsql.Messages;

namespace Npgsql.TypeHandlers
{
    internal class StringHandler : TypeHandler
    {
        static readonly string[] _pgNames = { "text" };
        internal override string[] PgNames { get { return _pgNames; } }
        internal override bool SupportsBinaryRead { get { return true; } }

        internal override void Read(DataRowMessageBase row, FieldDescription field, NpgsqlValue output)
        {
            output.SetTo(row.Buffer.ReadString(row.ColumnLen));
            row.PosInColumn = row.ColumnLen;
        }

        public long GetChars(DataRowMessageBase row, int decodedOffset, char[] output, int outputOffset, int decodedLen, FieldDescription field)
        {
            if (output == null)
            {
                // TODO: In non-sequential mode, getting the length can be implemented by decoding the entire
                // field - very inefficient. In sequential mode doing this prevents subsequent reading of the column.
                throw new NotImplementedException("Cannot get length of a text field for now");
            }

            if (decodedOffset < row.DecodedPosInColumn)
            {
                row.SeekInColumn(0);
                row.DecodedPosInColumn = 0;
            }

            if (decodedOffset > row.DecodedPosInColumn)
            {
                var charsToSkip = decodedOffset - row.DecodedPosInColumn;
                int bytesSkipped, charsSkipped;
                row.Buffer.SkipChars(charsToSkip, row.ColumnLen - row.PosInColumn, out bytesSkipped, out charsSkipped);
                row.PosInColumn += bytesSkipped;
                row.DecodedPosInColumn += charsSkipped;
                if (charsSkipped < charsToSkip)
                {
                    // TODO: What is the actual required behavior here?
                    throw new IndexOutOfRangeException();
                }
            }

            int bytesRead, charsRead;
            row.Buffer.ReadChars(output, outputOffset, decodedLen, row.ColumnLen - row.PosInColumn, out bytesRead, out charsRead);
            row.PosInColumn += bytesRead;
            row.DecodedPosInColumn += charsRead;
            return charsRead;
        }
    }
}
