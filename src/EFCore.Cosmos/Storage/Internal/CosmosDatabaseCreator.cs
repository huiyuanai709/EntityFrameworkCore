// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace Microsoft.EntityFrameworkCore.Cosmos.Storage.Internal
{
    public class CosmosDatabaseCreator : IDatabaseCreator
    {
        private readonly CosmosClientWrapper _cosmosClient;
        private readonly IModel _model;
        private readonly IModelDataTrackerFactory _modelDataTrackerFactory;
        private readonly IDatabase _database;

        public CosmosDatabaseCreator(
            CosmosClientWrapper cosmosClient,
            IModel model,
            IModelDataTrackerFactory modelDataTrackerFactory,
            IDatabase database)
        {
            _cosmosClient = cosmosClient;
            _model = model;
            _modelDataTrackerFactory = modelDataTrackerFactory;
            _database = database;
        }

        public bool EnsureCreated()
        {
            var created = _cosmosClient.CreateDatabaseIfNotExists();
            foreach (var entityType in _model.GetEntityTypes())
            {
                created |= _cosmosClient.CreateContainerIfNotExists(entityType.Cosmos().ContainerName, "__partitionKey");
            }

            if (created)
            {
                var modelDataTracker = _modelDataTrackerFactory.Create();
                foreach (var entityType in _model.GetEntityTypes())
                {
                    foreach (var targetSeed in entityType.GetData())
                    {
                        var entry = modelDataTracker.CreateEntry(targetSeed, entityType);
                        entry.EntityState = EntityState.Added;
                    }
                }

                _database.SaveChanges(modelDataTracker.GetEntriesToSave());
            }

            return created;
        }

        public async Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default)
        {
            var created = await _cosmosClient.CreateDatabaseIfNotExistsAsync(cancellationToken);
            foreach (var entityType in _model.GetEntityTypes())
            {
                created |= await _cosmosClient.CreateContainerIfNotExistsAsync(entityType.Cosmos().ContainerName, "__partitionKey", cancellationToken);
            }

            if (created)
            {
                var modelDataTracker = _modelDataTrackerFactory.Create();
                foreach (var entityType in _model.GetEntityTypes())
                {
                    foreach (var targetSeed in entityType.GetData())
                    {
                        var entry = modelDataTracker.CreateEntry(targetSeed, entityType);
                        entry.EntityState = EntityState.Added;
                    }
                }

                await _database.SaveChangesAsync(modelDataTracker.GetEntriesToSave(), cancellationToken);
            }

            return created;
        }

        public bool EnsureDeleted() => _cosmosClient.DeleteDatabase();

        public Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default)
            => _cosmosClient.DeleteDatabaseAsync(cancellationToken);

        public virtual bool CanConnect()
            => throw new NotImplementedException();

        public virtual Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
