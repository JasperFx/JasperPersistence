<!--Title:Extending Marten's Linq Support-->

**The Linq parsing and translation to Postgresql JSONB queries, not to mention Marten's own helpers and model, are not going
to be easily approachable for many people and this guide isn't exhaustive. Please feel free to ask for help in Marten's
Gitter room linked above.""

New for v0.8, Marten allows you to add Linq parsing and querying support for your own custom methods.
Using the (admittedly contrived) example from Marten's tests, say that you want to reuse a small part of a `Where()` clause across
different queries for "IsBlue()." First, write the method you want to be recognized by Marten's Linq support:

 <[sample:IsBlue]>

 Note a couple things here:

 1. If you're only using the method for Linq queries, it technically doesn't have to be implemented and never actually runs
 1. The methods do not have to be extension methods, but we're guessing that will be the most common usage of this

 Now, to create a custom Linq parser for the `IsBlue()` method, you need to create a custom implementation of the `IMethodCallParser`
 interface shown below:

 <[sample:IMethodCallParser]>

 The `IMethodCallParser` interface needs to match on method expressions that it could parse, and be able to turn the Linq expression into
 part of a Postgresql "where" clause. The custom Linq parser for `IsBlue()` is shown below:

<[sample:custom-extension-for-linq]>

Lastly, to plug in our new parser, we can add that to the `StoreOptions` object that we use to bootstrap a new `DocumentStore` as shown below:

<[sample:using_custom_linq_parser]>


