var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var encryptionKey = builder.AddParameter("field-encryption-key", secret: true);

var sqlServer = builder.AddSqlServer("sqlserver", sqlPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("appdb");

var api = builder.AddProject<Projects.Vanilla_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("FIELD_ENCRYPTION_KEY", encryptionKey);

builder.Build().Run();
