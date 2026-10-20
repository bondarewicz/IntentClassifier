namespace IntentClassifier.Domain.Matching;

using IntentClassifier.Domain.Intents;

// One candidate intent for a query. Rank 1 is the closest; similarity is cosine similarity.
public sealed record IntentMatch(int Rank, IntentId IntentId, string IntentName, double Similarity);
