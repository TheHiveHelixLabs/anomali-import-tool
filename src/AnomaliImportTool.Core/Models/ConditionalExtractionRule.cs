namespace AnomaliImportTool.Core.Models;

public enum ConditionOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    RegexMatch,
    GreaterThan,
    LessThan
}

public class ConditionalExtractionRule
{
    public string FieldName { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; } = ConditionOperator.Contains;
    public string Value { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; } = false;
} 