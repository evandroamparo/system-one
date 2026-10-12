using System.Text.Json.Serialization;

namespace SystemOne.Ollama;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ChoiceAnswer), "choice")]
[JsonDerivedType(typeof(NoulAnswer), "noul")]
[JsonDerivedType(typeof(ScoreAnswer), "score")]
public abstract record SystemOneAnswer;