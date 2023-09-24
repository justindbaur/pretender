# Pretender

## Benchmarks vs Competitors

> Doing a simple `Is.IsAny<string>()` or equivalent call.

| Method          | Mean         | Error      | StdDev       | Median       | Gen0   | Gen1   | Allocated |
|---------------- |-------------:|-----------:|-------------:|-------------:|-------:|-------:|----------:|
| MoqTest         | 46,155.49 ns | 919.804 ns | 1,999.579 ns | 45,122.63 ns | 1.3428 | 1.2207 |    8850 B |
| NSubstituteTest |  4,011.88 ns |  76.549 ns |    85.084 ns |  4,004.60 ns | 1.2360 | 0.0381 |    7760 B |
| PretenderTest   |     31.41 ns |   0.683 ns |     1.231 ns |     30.86 ns | 0.0268 |      - |     168 B |

## 0.1.0
- [x] Intercept `.Create` calls
- [x] Base implementation name off of `ITypeSymbol` derived info
- [x] Async/Task Support
- [x] Benchmark vs NSub & Moq
- [x] Property Support
- [x] Compiled Setup calls
- [x] Intercept Setup calls
- [x] Support Base MatcherAttribute
- [x] Special case AnyMatcher
- [x] Multiple uses of single type
- [x] Classes 
- [x] Constructor support
- [x] out parameters
- [x] Make our own delegate for matcher calls
- [x] Maybe not even use expressions in Setup
- [x] Use `Span<object?>` for `CallInfo` arguments
- [x] Multiple method overloads

## 0.2.0

- [ ] Check how many times a call has been made
- [ ] Take constructor args in `.Create()` call
- [ ] Respect `[Pretend]` attribute that is placed on a partial class
- [ ] A lot more benchmarks
- [ ] Property setter
- [ ] Static Abstract (not support but we should fake an implementation)
- [ ] More matchers
- [ ] Probably need a static matcher register :(
- [ ] Support Matcher arguments and fallback if impossible
- [ ] Obsolete public types that exist only for SourceGen
- [ ] And move to Pretender.Internals namespace
- [ ] pragma disable obsolete warnings
- [ ] More Behaviors
- [ ] Use CancellationToken everywhere


## 0.3.0

- [ ] Special case known good fake implementation over mocking
  - [ ] EphemeralDataProtectionProvider
  - [ ] TimeProvider
- [ ] Make Pretend Implementation castable to something you can get the Pretend out of


## Future Ideas
- [ ] What can I do with things that are `protected` or `sealed`
- [ ] Analyze all the things
- [ ] Custom awaitable (honestly don't think I care)
- [ ] Analyze constructor args them for likely match
- [ ] Debugger story
  - [ ] We do people want to step into
  - [ ] What objects will they inspect and how can it be helpful
- [ ] Documentation

