namespace ContextualDataIngestor
{
    internal class SqlRetrievalConfig
    {
        public required string ServerName { get; set; }
        public required string DatabaseName { get; set; }
        public required string TableName { get; set; }
    }
}