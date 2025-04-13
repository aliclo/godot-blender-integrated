using System.Collections.Generic;

public record ImportScriptParams {
    public string Name { get; set; }
    public List<CustomImportProperty> ImportProperties { get; set; }
}