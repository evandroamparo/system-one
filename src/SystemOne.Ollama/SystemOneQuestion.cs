using System.Text.Json.Serialization;

namespace SystemOne.Ollama;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ChoiceQuestion), "choice")]
[JsonDerivedType(typeof(NoulQuestion), "noul")]
[JsonDerivedType(typeof(ScoreQuestion), "score")]
public abstract record SystemOneQuestion(string Instructions);