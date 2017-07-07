using System;
using System.Collections.Generic;

namespace sapHowmuchConsumer
{
	public class JournalEntry
	{
		public DateTime ReferenceDate { get; set; }
		public DateTime DueDate { get; set; }
		public DateTime TaxDate { get; set; }
		public string Memo { get; set; }
		public IEnumerable<JournalEntryLine> Lines { get; set; }
	}

	public class JournalEntryLine
	{
		public string AccountCode { get; set; }
		public decimal Debit { get; set; }
		public decimal Credit { get; set; }
		public string ShortName { get; set; }
		public string LineMemo { get; set; }
		public DateTime DueDate { get; set; }
		public DateTime ReferenceDate { get; set; }
		public DateTime TaxDate { get; set; }
		public decimal BaseSum { get; set; }
	}
}