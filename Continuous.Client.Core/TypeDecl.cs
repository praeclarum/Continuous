using System;
using System.Collections.Generic;
using System.Text;

namespace Continuous.Client
{
    public abstract class TypeDecl
    {
        public DocumentRef Document { get; set; }
        public abstract string Name { get; }
        public abstract TextLoc StartLocation { get; }
        public abstract TextLoc EndLocation { get; }
        public abstract void SetTypeCode ();
    }

    public struct TextLoc
    {
		public const int MinLine = 1;
		public const int MinColumn = 1;

        public int Line;
        public int Column;

		public TextLoc (int line = MinLine, int column = MinColumn)
		{
			Line = line;
			Column = column;
		}

		public static bool operator < (TextLoc x, TextLoc y)
		{
			return x.Line < y.Line || x.Column < y.Column;
		}

		public static bool operator > (TextLoc x, TextLoc y)
		{
			return x.Line > y.Line || x.Column > y.Column;
		}

		public static bool operator <= (TextLoc x, TextLoc y)
		{
			return x.Line <= y.Line || x.Column <= y.Column;
		}

		public static bool operator >= (TextLoc x, TextLoc y)
		{
			return x.Line >= y.Line || x.Column >= y.Column;
		}

		public static bool operator == (TextLoc x, TextLoc y)
		{
			return x.Line == y.Line && x.Column == y.Column;
		}

		public static bool operator != (TextLoc x, TextLoc y)
		{
			return x.Line != y.Line || x.Column != y.Column;
		}

		public override bool Equals (object obj)
		{
			if (obj is TextLoc) {
				return this == (TextLoc)obj;
			}
			return false;
		}

		public override int GetHashCode ()
		{
			return Line.GetHashCode () + Column.GetHashCode ();
		}
	}

}
