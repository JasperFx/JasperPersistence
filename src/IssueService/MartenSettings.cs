namespace IssueService;

#region sample_integration_settings
public class MartenSettings
{
    public const string SECTION = "Marten";
    public string SchemaName { get; set; }
    public bool FromTests { get; set; } = false;
}
#endregion
