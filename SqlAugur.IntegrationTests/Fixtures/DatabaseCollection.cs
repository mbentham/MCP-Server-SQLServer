namespace SqlAugur.IntegrationTests.Fixtures;

[CollectionDefinition("Database")]
public sealed class DatabaseCollection : ICollectionFixture<SqlServerContainerFixture>;
