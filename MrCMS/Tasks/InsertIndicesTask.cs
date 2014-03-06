﻿using MrCMS.Entities;
using MrCMS.Indexing.Management;
using MrCMS.Services;
using NHibernate;
using Ninject;

namespace MrCMS.Tasks
{
    internal class InsertIndicesTask<T> : IndexManagementTask<T> where T : SiteEntity
    {
        public InsertIndicesTask(ISession session, IKernel kernel, IIndexService indexService)
            : base(session, kernel, indexService)
        {
        }

        protected override void ExecuteLogic(IIndexManagerBase manager, T entity)
        {
            manager.Insert(entity);
        }

        protected override LuceneOperation Operation
        {
            get { return LuceneOperation.Insert; }
        }
    }
}