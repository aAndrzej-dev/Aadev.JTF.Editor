using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor;
public class IntelligentSuggestionsProvider
{
    internal List<IntelligentSuggestionKey> keys = new List<IntelligentSuggestionKey>();


    internal List<IntelligentSuggestion>? GetSuggestions(ReadOnlySpan<char> key)
    {
        return GetKey(key)?.suggestions;
    }
    internal IntelligentSuggestionKey? GetKey(ReadOnlySpan<char> key)
    {
        Span<IntelligentSuggestionKey> keysSpan = CollectionsMarshal.AsSpan(keys);
        for (int i = 0; i < keysSpan.Length; i++)
        {
            if (key.SequenceEqual(keysSpan[i].key))
            {
                return keysSpan[i];
            }
        }

        return null;
    }
    internal IntelligentSuggestionKey GetKeyOrCreate(ReadOnlySpan<char> key, Type suggestionType)
    {
        Span<IntelligentSuggestionKey> keysSpan = CollectionsMarshal.AsSpan(keys);
        for (int i = 0; i < keysSpan.Length; i++)
        {
            if (key.SequenceEqual(keysSpan[i].key))
            {
                return keysSpan[i];
            }
        }

        IntelligentSuggestionKey newKey = new IntelligentSuggestionKey(key.ToString(), suggestionType);

        keys.Add(newKey);

        return newKey;
    }
}
internal class IntelligentSuggestionKey
{
    internal readonly string key;
    internal readonly Type suggestionType;
    internal readonly List<IntelligentSuggestion> suggestions;

    public IntelligentSuggestionKey(string key, Type suggestionType)
    {
        suggestions = new List<IntelligentSuggestion>();
        this.key = key;
        this.suggestionType = suggestionType;
    }
}
internal class IntelligentSuggestion : IJtSuggestion
{
    internal JValue source;
    internal IntelligentSuggestionKey key;

    public IntelligentSuggestion(JValue source, IntelligentSuggestionKey key)
    {
        this.source = source;
        this.key = key;
    }

    public Type SuggestionType => key.suggestionType;

    public string? DisplayName { get => StringValue; set => throw new NotSupportedException(); }

    public string? StringValue => source.Value?.ToString();

    public override string? ToString() => StringValue;

    public T GetValue<T>()
    {
        if (typeof(T) != key.suggestionType)
            throw new InvalidCastException();

        return (T)source.Value!;
    }

    public object? GetValue()
    {
        return Convert.ChangeType(source.Value, key.suggestionType);
    }

    public void SetValue<T>(T value)
    {
        throw new NotSupportedException();
    }

    public void SetValue(object? value)
    {
        throw new NotSupportedException();
    }
}
