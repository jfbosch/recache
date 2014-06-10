super-cache
===========

There are lots of caching options out there, but if you need something really simple that you can get running within 5 minutes, without any external dependencies, this might just be for you.

Why?
====
Because sometimes an in-memory cache is really the bettor option over an out-of-process, external cache service such as redis, memcached, and the like.

The skinny on Features
========
* Fast
* Generic
* Transparent
* Concurrency Safe
* Expiry Configuration
* Max Size Indicator
* Auto Flush Stale Entries
* Optionally Self-Refreshing

What does it do?
================
Quite simply, it caches any .Net value in memory against a key. This makes for really, really fast retrieval of that value when asking for it via the key.
Both the key and the value are generic types, so the cache can be of any types you would like.
e.g. Cache<int, string>, or Cache<string, MyFunkyClass>.

The cache is transparent. This means you set it up with a loader func, and when you hit the cache the first time, before it has been cached, it will invoke your provided loader func to go and fetch the value from anywhere, in any way you would like. It then caches the value, and returns it. Next time you hit the same key, providing the value is not yet stale as per your configured expiry timeout, you will get the same value back, but just much, much faster than the first time.

There is also a self-refreshing version.
e.g. SelfRefreshingCache<int, string>.
Once a key is in the cache, it will go and refresh itself on a configurable schedule. This means that the clients of the cache, after the very first load, will never see the performance hit again of fetching the fresh values through the loader func from the data source.
It works on a generational model. While generation 2 is being fetch asynchronously in the background, the cache will keep serving up generation 1 to any clients that hit that key. The moment generation 2 is available, it replaces generation 1. In this way, the cache stays fresh, but there is no occasional delay as a client hits an expired cache entry.

It is built on ConcurrentDictionary<TKey, TValue>, so it is concurrency / thread safe.

Samples
=======
(Still to come)
(but really, it is very simple, just try one of the constructors and the class will show you what to do.)

Does it work?
=============
Yes it works exceptionally well. We have it running in production, caching anything from lookups to search results to templates to types for dependency injection. It has given us incredible performance improvements.
Once you start using it for one thing, you will probably find other places to plug it in as well.

How to get it?
==============
At the moment there is no nuget package for it. We might add it in the future.
For now, just grab this tiny project, build it, reference it, and off you go.
