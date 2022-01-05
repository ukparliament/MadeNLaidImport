namespace MadeNLaidImport
{
    using System;
    class StatutoryInstrument
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string WorkPackageId { get; set; }
        public string ProcedureId { get; set; }
        public string ProcedureName { get; set; }
        public string LayingBodyName { get; set; }
        public DateTimeOffset LaidDate { get; set; }
        public DateTimeOffset MadeDate { get; set; }
        public string Link { get; set; }
    }
}
