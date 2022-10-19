using CarinaStudio.AppSuite.Data;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.ULogViewer.Logs.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CarinaStudio.ULogViewer.ViewModels.Analysis;

/// <summary>
/// Set of rule for key log analysis.
/// </summary>
class KeyLogAnalysisRuleSet : DisplayableLogAnalysisRuleSet<KeyLogAnalysisRuleSet.Rule>
{
    /// <summary>
    /// Analysis rule.
    /// </summary>
    public class Rule : IEquatable<Rule>
    {
        /// <summary>
        /// Initialize new <see cref="Rule"/> instance.
        /// </summary>
        /// <param name="pattern">Pattern to match log.</param>
        /// <param name="level">Level to match log.</param>
        /// <param name="conditions">Conditions to match log.</param>
        /// <param name="resultType">Type of analysis result to be generated when pattern matched.</param>
        /// <param name="message">Formatted message to be generated when pattern matched.</param>
        public Rule(Regex pattern, Logs.LogLevel level, IEnumerable<DisplayableLogAnalysisCondition> conditions, DisplayableLogAnalysisResultType resultType, string message)
        {
            this.Conditions = conditions is IList<DisplayableLogAnalysisCondition> list
                ? list.AsReadOnly()
                : conditions.ToArray().AsReadOnly();
            this.Level = level;
            this.Message = message;
            this.Pattern = pattern;
            this.ResultType = resultType;
        }

        /// <summary>
        /// Conditions to match log.
        /// </summary>
        public IList<DisplayableLogAnalysisCondition> Conditions { get; }

        /// <inheritdoc/>
        public bool Equals(Rule? rule) =>
            rule != null
            && rule.Conditions.SequenceEqual(this.Conditions)
            && rule.Level == this.Level
            && rule.Message == this.Message
            && rule.Pattern.ToString() == this.Pattern.ToString()
            && rule.Pattern.Options == this.Pattern.Options
            && rule.ResultType == this.ResultType;

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is Rule rule && this.Equals(rule);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            this.Pattern.ToString().GetHashCode();
        
        /// <summary>
        /// Get level to match log.
        /// </summary>
        public Logs.LogLevel Level { get; }

        /// <summary>
        /// Get formatted message to be generated when pattern matched.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Get pattern to match log.
        /// </summary>
        public Regex Pattern { get; }

        /// <summary>
        /// Get type of analysis result to be generated when pattern matched.
        /// </summary>
        public DisplayableLogAnalysisResultType ResultType { get; }
    }


    // Constructor.
    KeyLogAnalysisRuleSet(IULogViewerApplication app, string id) : base(app, id)
    { }


    /// <summary>
    /// Initialize new <see cref="KeyLogAnalysisRuleSet"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    public KeyLogAnalysisRuleSet(IULogViewerApplication app) : base(app, KeyLogAnalysisRuleSetManager.Default.GenerateProfileId())
    { }


    /// <summary>
    /// Initialize new <see cref="KeyLogAnalysisRuleSet"/> instance.
    /// </summary>
    /// <param name="template">Template rule set.</param>
    /// <param name="name">Name.</param>
    public KeyLogAnalysisRuleSet(KeyLogAnalysisRuleSet template, string name) : base(template, name, KeyLogAnalysisRuleSetManager.Default.GenerateProfileId())
    { }


    /// <inheritdoc/>
    internal override void ChangeId() =>
        this.Id = KeyLogAnalysisRuleSetManager.Default.GenerateProfileId();
    

    /// <summary>
    /// Load profile from file.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="fileName">File name.</param>
    /// <param name="checkType">True to check whether type written in file is correct or not.</param>
    /// <returns>Task of loading profile.</returns>
    public static async Task<KeyLogAnalysisRuleSet> LoadAsync(IULogViewerApplication app, string fileName, bool checkType)
    {
        // load JSON data
        using var jsonDocument = await ProfileExtensions.IOTaskFactory.StartNew(() =>
        {
            using var reader = new StreamReader(fileName, System.Text.Encoding.UTF8);
            return JsonDocument.Parse(reader.ReadToEnd());
        });
        var element = jsonDocument.RootElement;
        if (element.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Root element must be an object.");
        if (checkType)
        {
            if (!element.TryGetProperty("Type", out var jsonValue)
                || jsonValue.ValueKind != JsonValueKind.String
                || jsonValue.GetString() != nameof(KeyLogAnalysisRuleSet))
            {
                throw new ArgumentException($"Invalid type: {jsonValue}.");
            }
        }
        
        // get ID
        var id = element.TryGetProperty(nameof(Id), out var jsonProperty) && jsonProperty.ValueKind == JsonValueKind.String
            ? jsonProperty.GetString().AsNonNull()
            : KeyLogAnalysisRuleSetManager.Default.GenerateProfileId();
        
        // load
        var profile = new KeyLogAnalysisRuleSet(app, id);
        profile.Load(element);
        return profile;
    }


    /// <inheritdoc/>
    protected override void OnLoad(JsonElement element)
    { 
        var hasType = false;
        foreach (var jsonProperty in element.EnumerateObject())
        {
            switch (jsonProperty.Name)
            {
                case nameof(Icon):
                    if (jsonProperty.Value.ValueKind == JsonValueKind.String
                        && Enum.TryParse<LogProfileIcon>(jsonProperty.Value.GetString(), out var icon))
                    {
                        this.Icon = icon;
                    }
                    break;
                case nameof(IconColor):
                    if (jsonProperty.Value.ValueKind == JsonValueKind.String
                        && Enum.TryParse<LogProfileIconColor>(jsonProperty.Value.GetString(), out var iconColor))
                    {
                        this.IconColor = iconColor;
                    }
                    break;
                case nameof(Id):
                    break;
                case nameof(Name):
                    this.Name = jsonProperty.Value.GetString();
                    break;
                case nameof(Rules):
                    this.Rules = new List<Rule>().Also(patterns =>
                    {
                        var useCompiledRegex = this.Application.Configuration.GetValueOrDefault(ConfigurationKeys.UseCompiledRegex);
                        foreach (var jsonValue in jsonProperty.Value.EnumerateArray())
                        {
                            var ignoreCase = jsonValue.TryGetProperty("IgnoreCase", out var ignoreCaseProperty) && ignoreCaseProperty.ValueKind == JsonValueKind.True;
                            var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                            if (useCompiledRegex)
                                options |= RegexOptions.Compiled;
                            var regex = new Regex(jsonValue.GetProperty(nameof(Rule.Pattern)).GetString()!, options);
                            var level = jsonValue.TryGetProperty(nameof(Rule.Level), out var jsonLevel)
                                && jsonLevel.ValueKind == JsonValueKind.String
                                && Enum.TryParse<Logs.LogLevel>(jsonLevel.GetString(), out var candLevel)
                                    ? candLevel
                                    : Logs.LogLevel.Undefined;
                            var conditions = jsonValue.TryGetProperty(nameof(Rule.Conditions), out var conditionsProperty) && conditionsProperty.ValueKind == JsonValueKind.Array
                                ? new List<DisplayableLogAnalysisCondition>().Also(it =>
                                {
                                    foreach (var conditionElement in conditionsProperty.EnumerateArray())
                                    {
                                        if (DisplayableLogAnalysisCondition.TryLoad(conditionElement, out var condition))
                                            it.Add(condition);
                                    }
                                }).AsReadOnly()
                                : (IList<DisplayableLogAnalysisCondition>)new DisplayableLogAnalysisCondition[0];
                            var formattedMessage = jsonValue.GetProperty(nameof(Rule.Message)).GetString().AsNonNull();
                            var resultType = jsonValue.TryGetProperty(nameof(Rule.ResultType), out var resultTypeValue) 
                                && resultTypeValue.ValueKind == JsonValueKind.String
                                && Enum.TryParse<DisplayableLogAnalysisResultType>(resultTypeValue.GetString(), out var type)
                                    ? type
                                    : DisplayableLogAnalysisResultType.Information;
                            patterns.Add(new(regex, level, conditions, resultType, formattedMessage));
                        }
                    });
                    break;
                case "Type":
                    if (jsonProperty.Value.GetString() != nameof(KeyLogAnalysisRuleSet))
                        throw new ArgumentException($"Incorrect type: {jsonProperty.Value.GetString()}");
                    hasType = true;
                    break;
            }
        }
        if (!hasType)
            this.IsDataUpgraded = true;
    }


    /// <inheritdoc/>
    protected override void OnSave(Utf8JsonWriter writer, bool includeId)
    {
        writer.WriteStartObject();
        writer.WriteString("Type", nameof(KeyLogAnalysisRuleSet));
        writer.WriteString(nameof(Icon), this.Icon.ToString());
        if (this.IconColor != LogProfileIconColor.Default)
            writer.WriteString(nameof(IconColor), this.IconColor.ToString());
        if (includeId)
            writer.WriteString(nameof(Id), this.Id);
        this.Name?.Let(it =>
            writer.WriteString(nameof(Name), it));
        if (this.Rules.IsNotEmpty())
        {
            writer.WritePropertyName(nameof(Rules));
            writer.WriteStartArray();
            foreach (var rule in this.Rules)
            {
                writer.WriteStartObject();
                rule.Pattern.Let(it =>
                {
                    writer.WriteString(nameof(Rule.Pattern), it.ToString());
                    if ((it.Options & RegexOptions.IgnoreCase) != 0)
                        writer.WriteBoolean("IgnoreCase", true);
                });
                if (rule.Level != Logs.LogLevel.Undefined)
                    writer.WriteString(nameof(Rule.Level), rule.Level.ToString());
                if (rule.Conditions.IsNotEmpty())
                {
                    writer.WritePropertyName(nameof(Rule.Conditions));
                    writer.WriteStartArray();
                    foreach (var condition in rule.Conditions)
                        condition.Save(writer);
                    writer.WriteEndArray();
                }
                writer.WriteString(nameof(Rule.Message), rule.Message);
                writer.WriteString(nameof(Rule.ResultType), rule.ResultType.ToString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}