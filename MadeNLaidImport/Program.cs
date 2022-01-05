namespace MadeNLaidImport
{
    using System;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using VDS.RDF;
    using VDS.RDF.Query;
    using VDS.RDF.Storage;

    class Program
    {
        static void Main(string[] args)
        {
            string sparqlEndpoint = ConfigurationManager.AppSettings["SparqlEndpoint"];
            int dayOffset = int.Parse(ConfigurationManager.AppSettings["DayOffset"]);
            var from_date = DateTime.Today.AddDays(dayOffset).ToString("yyyy-MM-d");
            var to_date = DateTime.Today.ToString("yyyy-MM-d");
            string query = $@"PREFIX rdfs:<http://www.w3.org/2000/01/rdf-schema#>
                PREFIX :<https://id.parliament.uk/schema/>
                PREFIX id:<https://id.parliament.uk/>
                select distinct ?SI ?SIname ?workPackage ?procedureId ?Procedure ?layingBodyName ?Madedate ?LaidDate ?Link where {{
                  ?SI a :StatutoryInstrumentPaper.
                  ?SI rdfs:label ?SIname;
                      :laidThingHasLaying/:layingHasLayingBody/:name ?layingBodyName;
                      :laidThingHasLaying/:layingDate ?LaidDate.
                OPTIONAL {{?SI :workPackagedThingHasWorkPackagedThingWebLink ?Link.}}
                OPTIONAL {{?SI :statutoryInstrumentPaperMadeDate ?Madedate.}}
      	          ?SI :workPackagedThingHasWorkPackage ?workPackage .
    	          ?workPackage :workPackageHasProcedure ?procedureId.
                  ?procedureId :name ?Procedure.
                FILTER(?procedureId IN (id:iWugpxMn, id:5S6p4YsP))
                FILTER(str(?LaidDate) >= '{from_date}' && str(?LaidDate) <= '{to_date}')
                }}";
            using (var connector = new SparqlConnector(new Uri(sparqlEndpoint)))
            {
                var results = connector.Query(query) as SparqlResultSet;
                if (results.Any())
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString;
                    Console.WriteLine($"Sql: {connectionString}");
                    SqlConnection connection = new SqlConnection(connectionString);
                    connection.Open();

                    foreach (var result in results)
                    {
                        INode node;
                        StatutoryInstrument si = new StatutoryInstrument();
                        result.TryGetValue("SI", out node);
                        si.Id = (node as UriNode).Uri.ToString();
                        result.TryGetValue("SIname", out node);
                        si.Name = (node as LiteralNode).Value.Trim();
                        result.TryGetValue("workPackage", out node);
                        si.WorkPackageId = (node as UriNode).Uri.ToString().Trim();
                        result.TryGetValue("procedureId", out node);
                        si.ProcedureId = (node as UriNode).Uri.ToString().Trim();
                        result.TryGetValue("Procedure", out node);
                        si.ProcedureName = (node as LiteralNode).Value.Trim();
                        result.TryGetValue("layingBodyName", out node);
                        si.LayingBodyName = (node as LiteralNode).Value.Trim();
                        result.TryGetValue("Madedate", out node);
                        si.MadeDate = DateTime.Parse((node as LiteralNode).Value.Trim());
                        result.TryGetValue("LaidDate", out node);
                        si.LaidDate = DateTime.Parse((node as LiteralNode).Value.Trim());
                        result.TryGetValue("Link", out node);
                        si.Link = (node as UriNode).Uri.ToString().Trim();

                        using (SqlCommand cmd = new SqlCommand("Add to database", connection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = "InsertUpdateMadeNLaidStatutoryInstrument";
                            cmd.Parameters.AddWithValue("@StatutoryInstrumentName", si.Name != null ? (object)si.Name : DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcedureName", si.ProcedureName != null ? (object)si.ProcedureName : DBNull.Value);
                            cmd.Parameters.AddWithValue("@LayingBodyName", si.LayingBodyName != null ? (object)si.LayingBodyName : DBNull.Value);
                            cmd.Parameters.AddWithValue("@MadeDate", si.MadeDate != null ? (object)si.MadeDate : DBNull.Value);
                            cmd.Parameters.AddWithValue("@LaidDate", si.LaidDate != null ? (object)si.LaidDate : DBNull.Value);
                            cmd.Parameters.AddWithValue("@StatutoryInstrumentUri", si.Id != null ? (object)si.Id : DBNull.Value);
                            cmd.Parameters.AddWithValue("@WorkPackageUri", si.WorkPackageId != null ? (object)si.WorkPackageId : DBNull.Value);
                            cmd.Parameters.AddWithValue("@TnaUri", si.Link != null ? (object)si.Link : DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsTweeted", (object)0);
                            cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 50).Direction = ParameterDirection.Output;
                            cmd.ExecuteNonQuery();
                            string msg = cmd.Parameters["@Message"].Value.ToString();
                            Console.WriteLine($"Title: {si.Id}, {msg}");
                        }
                    }
                    connection.Close();
                }
            }

        }
    }
}
