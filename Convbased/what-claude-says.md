  ---
  Root Cause: Dedup Key Mismatch Between Source-Gen and Scanner Paths

  What happens step by step (Case 3: no IHandleMessages<>, default scanner, [Handler] + AddAll())
  What happens step by step (Case 3: no IHandleMessages<>, default scanner, [Handler] + AddAll())

  Step 1 — Source-gen path (AddAll() interceptor)

  The Roslyn interceptor in AddHandlerInterceptor.Emitter.cs generates a file-scoped adapter class at compile time:

  sealed file class MyHandler_Adapter : global::NServiceBus.IHandleMessages<ReproMessage>
  {
      // delegates to ReproMessageHandler
  }

  It then generates a call using the 3-parameter overload:
  registry.AddMessageHandlerForMessage<MyHandler_Adapter, ReproMessage, ReproMessageHandler>();

  In MessageHandlerRegistry, this overload deduplicates on THandler (the 3rd type arg — the original handler):
  dedup key = { HandlerType = typeof(ReproMessageHandler), MessageType = typeof(ReproMessage) }

  Step 2 — Assembly scanner path

  file-scoped types are a compile-time visibility restriction only. At runtime the adapter exists in the assembly with a mangled CLR name
  (e.g. <...>F__MyHandler_Adapter) and appears in Assembly.GetTypes(). It implements IHandleMessages<ReproMessage>, is not abstract, and
  is not generic — so IsMessageHandler() returns true.

  The scanner finds it and calls AddHandlerWithReflection(adapterType) → AddHandler<Adapter>() → the 2-parameter overload (THandler =
  TAdapter):
  dedup key = { HandlerType = typeof(MyHandler_Adapter), MessageType = typeof(ReproMessage) }

  Step 3 — Deduplication failure

  ┌──────────────────────┬─────────────────────────────┐
  │         Path         │  HandlerType in dedup key   │
  ├──────────────────────┼─────────────────────────────┤
  │ AddAll() interceptor │ typeof(ReproMessageHandler) │
  ├──────────────────────┼─────────────────────────────┤
  │ Assembly scanner     │ typeof(MyHandler_Adapter)   │
  └──────────────────────┴─────────────────────────────┘

  Two different types → both keys pass deduplicationSet.Add(...) → two factories registered → handler fires twice.

  ---
  Why the other cases are fine

  ┌───────────────────────────────────────┬───────────────────────────────────────────────────────────────────────────────────────────┐
  │                 Case                  │                                      Reason for once                                      │
  ├───────────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────┤
  │ Scanner disabled                      │ Adapter never discovered by scanner                                                       │
  ├───────────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────┤
  │ IHandleMessages<> present (IsMixed =  │ Interceptor explicitly skips mixed handlers — no adapter generated, scanner only finds    │
  │ true)                                 │ the original type once                                                                    │
  └───────────────────────────────────────┴───────────────────────────────────────────────────────────────────────────────────────────┘

  ---
  Where to look in the source

  - src/NServiceBus.Core.Analyzer/Handlers/Handlers.Emitter.cs — generates the file-scoped adapter
  - src/NServiceBus.Core/Unicast/MessageHandlerRegistry.cs — AddMessageHandlerForMessage<TAdapter, TMessage, THandler>() uses THandler as
  dedup key, but scanner path calls the 2-param overload with THandler = TAdapter
  - IsMessageHandler(Type) — has no exclusion for compiler-generated / file-scoped types

  The fix would be to teach IsMessageHandler (or AddScannedHandlers) to exclude generated adapter types — e.g. by checking for the
  [CompilerGenerated] attribute or a private marker attribute placed on all emitted adapters.
