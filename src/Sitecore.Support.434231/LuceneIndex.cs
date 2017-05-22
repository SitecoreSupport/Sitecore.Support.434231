using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Abstractions;
using Sitecore.ContentSearch.Events;
using Sitecore.ContentSearch.Maintenance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Sitecore.Support.ContentSearch.LuceneProvider
{
    public class LuceneIndex : Sitecore.ContentSearch.LuceneProvider.LuceneIndex
    {
        // Methods
        protected LuceneIndex(string name) : base(name)
        {
        }

        public LuceneIndex(string name, string folder, IIndexPropertyStore propertyStore) : this(name, folder, propertyStore, null)
        {
        }

        public LuceneIndex(string name, string folder, IIndexPropertyStore propertyStore, string group) : base(name, folder, propertyStore, group)
        {
        }

        public override void Delete(IIndexableId indexableId)
        {
            this.PerformDelete(indexableId, IndexingOptions.Default);
        }

        public override void Delete(IIndexableUniqueId indexableUniqueId)
        {
            this.PerformDelete(indexableUniqueId, IndexingOptions.Default);
        }

        public override void Delete(IIndexableId indexableId, IndexingOptions indexingOptions)
        {
            this.PerformDelete(indexableId, indexingOptions);
        }

        public override void Delete(IIndexableUniqueId indexableUniqueId, IndexingOptions indexingOptions)
        {
            this.PerformDelete(indexableUniqueId, indexingOptions);
        }

        private void PerformDelete(IIndexableId indexableId, IndexingOptions indexingOptions)
        {
            base.VerifyNotDisposed();
            if (base.ShouldStartIndexing(indexingOptions))
            {
                using (IProviderUpdateContext context = this.CreateUpdateContext())
                {
                    foreach (IProviderCrawler crawler in base.Crawlers)
                    {
                        crawler.Delete(context, indexableId, indexingOptions);
                    }
                    context.Commit();
                }
            }
        }

        private void PerformDelete(IIndexableUniqueId indexableUniqueId, IndexingOptions indexingOptions)
        {
            base.VerifyNotDisposed();
            if (base.ShouldStartIndexing(indexingOptions))
            {
                using (IProviderUpdateContext context = this.CreateUpdateContext())
                {
                    foreach (IProviderCrawler crawler in base.Crawlers)
                    {
                        crawler.Delete(context, indexableUniqueId, indexingOptions);
                    }
                    context.Commit();
                }
            }
        }

        protected override void PerformRefresh(IIndexable indexableStartingPoint, IndexingOptions indexingOptions, CancellationToken cancellationToken)
        {
            base.VerifyNotDisposed();
            if (base.ShouldStartIndexing(indexingOptions))
            {
                lock (base.indexUpdateLock)
                {
                    if (base.Crawlers.Any<IProviderCrawler>(c => c.HasItemsToIndex()))
                    {
                        using (IProviderUpdateContext context = this.CreateUpdateContext())
                        {
                            foreach (IProviderCrawler crawler in base.Crawlers)
                            {
                                crawler.RefreshFromRoot(context, indexableStartingPoint, indexingOptions, cancellationToken);
                            }
                            context.Commit();
                        }
                    }
                }
            }
        }

        private void PerformUpdate(IIndexableUniqueId indexableUniqueId, IndexingOptions indexingOptions)
        {
            base.VerifyNotDisposed();
            if (base.ShouldStartIndexing(indexingOptions))
            {
                using (IProviderUpdateContext context = this.CreateUpdateContext())
                {
                    foreach (IProviderCrawler crawler in base.Crawlers)
                    {
                        crawler.Update(context, indexableUniqueId, indexingOptions);
                    }
                    context.Commit();
                }
            }
        }

        private void PerformUpdate(IEnumerable<IIndexableUniqueId> indexableUniqueIds, IndexingOptions indexingOptions)
        {
            if (base.ShouldStartIndexing(indexingOptions))
            {
                IEvent instance = base.Locator.GetInstance<IEvent>();
                instance.RaiseEvent("indexing:start", new object[] { this.Name, false });
                IndexingStartedEvent event3 = new IndexingStartedEvent
                {
                    IndexName = this.Name,
                    FullRebuild = false
                };
                base.Locator.GetInstance<IEventManager>().QueueEvent<IndexingStartedEvent>(event3);
                Action<IIndexableUniqueId> body = null;
                Action<IIndexableUniqueId> action2 = null;
                using (IProviderUpdateContext context = this.CreateUpdateContext())
                {
                    if (context.IsParallel)
                    {
                        if (body == null)
                        {
                            if (action2 == null)
                            {
                                action2 = delegate (IIndexableUniqueId uniqueId) {
                                    if (this.ShouldStartIndexing(indexingOptions))
                                    {
                                        foreach (IProviderCrawler crawler in this.Crawlers)
                                        {
                                            crawler.Update(context, uniqueId, indexingOptions);
                                        }
                                    }
                                };
                            }
                            body = action2;
                        }
                        Parallel.ForEach<IIndexableUniqueId>(indexableUniqueIds, context.ParallelOptions, body);
                        if (!base.ShouldStartIndexing(indexingOptions))
                        {
                            context.Commit();
                            return;
                        }
                    }
                    else
                    {
                        foreach (IIndexableUniqueId id in indexableUniqueIds)
                        {
                            if (!base.ShouldStartIndexing(indexingOptions))
                            {
                                context.Commit();
                                return;
                            }
                            foreach (IProviderCrawler crawler in base.Crawlers)
                            {
                                crawler.Update(context, id, indexingOptions);
                            }
                        }
                    }
                    context.Commit();
                }
                instance.RaiseEvent("indexing:end", new object[] { this.Name, false });
                IndexingFinishedEvent event4 = new IndexingFinishedEvent
                {
                    IndexName = this.Name,
                    FullRebuild = false
                };
                base.Locator.GetInstance<IEventManager>().QueueEvent<IndexingFinishedEvent>(event4);
            }
        }

        public override void Refresh(IIndexable indexableStartingPoint)
        {
            this.PerformRefresh(indexableStartingPoint, IndexingOptions.Default, CancellationToken.None);
        }

        public override void Refresh(IIndexable indexableStartingPoint, IndexingOptions indexingOptions)
        {
            this.PerformRefresh(indexableStartingPoint, indexingOptions, CancellationToken.None);
        }

        public override void Update(IIndexableUniqueId indexableUniqueId)
        {
            this.PerformUpdate(indexableUniqueId, IndexingOptions.Default);
        }

        public override void Update(IEnumerable<IIndexableUniqueId> indexableUniqueIds)
        {
            this.PerformUpdate(indexableUniqueIds, IndexingOptions.Default);
        }

        public override void Update(IIndexableUniqueId indexableUniqueId, IndexingOptions indexingOptions)
        {
            this.PerformUpdate(indexableUniqueId, indexingOptions);
        }

        public override void Update(IEnumerable<IIndexableUniqueId> indexableUniqueIds, IndexingOptions indexingOptions)
        {
            this.PerformUpdate(indexableUniqueIds, indexingOptions);
        }
    }

}