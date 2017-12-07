﻿using System;
using System.Collections.Generic;
using System.Linq;
using NPoco;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Persistence.Querying;
using Umbraco.Core.Persistence.UnitOfWork;

namespace Umbraco.Core.Persistence.Repositories.Implement
{
    /// <summary>
    /// Simple abstract ReadOnly repository used to simply have PerformGet and PeformGetAll with an underlying cache
    /// </summary>
    internal abstract class SimpleGetRepository<TId, TEntity, TDto> : NPocoRepositoryBase<TId, TEntity>
        where TEntity : class, IAggregateRoot
        where TDto: class
    {

        protected SimpleGetRepository(IScopeUnitOfWork work, CacheHelper cache, ILogger logger)
            : base(work, cache, logger)
        {
        }

        protected abstract TEntity ConvertToEntity(TDto dto);
        protected abstract object GetBaseWhereClauseArguments(TId id);
        protected abstract string GetWhereInClauseForGetAll();

        protected virtual IEnumerable<TDto> PerformFetch(Sql sql)
        {
            return Database.Fetch<TDto>(sql);
        }

        protected override TEntity PerformGet(TId id)
        {
            var sql = GetBaseQuery(false);
            sql.Where(GetBaseWhereClause(), GetBaseWhereClauseArguments(id));

            var dto = PerformFetch(sql).FirstOrDefault();
            if (dto == null)
                return null;

            var entity = ConvertToEntity(dto);

            var dirtyEntity = entity as Entity;
            if (dirtyEntity != null)
            {
                // reset dirty initial properties (U4-1946)
                dirtyEntity.ResetDirtyProperties(false);
            }

            return entity;
        }

        protected override IEnumerable<TEntity> PerformGetAll(params TId[] ids)
        {
            var sql = Sql().From<TEntity>();

            if (ids.Any())
            {
                sql.Where(GetWhereInClauseForGetAll(), new { /*ids =*/ ids });
            }

            return Database.Fetch<TDto>(sql).Select(ConvertToEntity);
        }

        protected sealed override IEnumerable<TEntity> PerformGetByQuery(IQuery<TEntity> query)
        {
            var sqlClause = GetBaseQuery(false);
            var translator = new SqlTranslator<TEntity>(sqlClause, query);
            var sql = translator.Translate();
            return Database.Fetch<TDto>(sql).Select(ConvertToEntity);
        }

        #region Not implemented and not required

        protected sealed override IEnumerable<string> GetDeleteClauses()
        {
            throw new NotImplementedException();
        }

        protected sealed override Guid NodeObjectTypeId
        {
            get { throw new NotImplementedException(); }
        }

        protected sealed override void PersistNewItem(TEntity entity)
        {
            throw new NotImplementedException();
        }

        protected sealed override void PersistUpdatedItem(TEntity entity)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}