﻿using System;
using Microsoft.EntityFrameworkCore;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    /// <summary>
    /// Extension methods that provide access to upsert commands on a DbContext
    /// </summary>
    public static class ObsoleteUpsertExtensions
    {
        /// <summary>
        /// Attempt to insert an entity to the database, or update it if one already exists
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbContext">The data context used to connect to the database</param>
        /// <param name="entity">The entity that is being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        [Obsolete("This extension method was moved to the Microsoft.EntityFrameworkCore namespace")]
        public static UpsertCommandBuilder<TEntity> Upsert<TEntity>(this DbContext dbContext, TEntity entity)
            where TEntity : class
        {
            return UpsertRange(dbContext, entity);
        }

        /// <summary>
        /// Attempt to insert an array of entities to the database, or update them if they already exist
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being upserted</typeparam>
        /// <param name="dbContext">The data context used to connect to the database</param>
        /// <param name="entities">The entities that are being upserted</param>
        /// <returns>The upsert command builder that is used to configure and run the upsert operation</returns>
        [Obsolete("This extension method was moved to the Microsoft.EntityFrameworkCore namespace")]
        public static UpsertCommandBuilder<TEntity> UpsertRange<TEntity>(this DbContext dbContext, params TEntity[] entities)
            where TEntity : class
        {
            return new UpsertCommandBuilder<TEntity>(dbContext, entities);
        }
    }
}
