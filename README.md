# Pretender

## Benchmarks vs Competitors

| Method          | Mean         | Error      | StdDev       | Median       | Gen0   | Gen1   | Allocated |
|---------------- |-------------:|-----------:|-------------:|-------------:|-------:|-------:|----------:|
| MoqTest         | 46,155.49 ns | 919.804 ns | 1,999.579 ns | 45,122.63 ns | 1.3428 | 1.2207 |    8850 B |
| NSubstituteTest |  4,011.88 ns |  76.549 ns |    85.084 ns |  4,004.60 ns | 1.2360 | 0.0381 |    7760 B |
| PretenderTest   |     31.41 ns |   0.683 ns |     1.231 ns |     30.86 ns | 0.0268 |      - |     168 B |

## To-do list

In no particular order

- [x] Intercept .Create calls
    - [x] Base Implementation name off of ITypeSymbol derived info
    - [ ] Should allow naming customization
    - [ ] Respect [Pretend] attribute that is placed on a partial class
- [x] Async/Task Support
    - [ ] Custom awaitable (honestly don't think I care)
- [ ] Classes (maybe already do)
- [x] Benchmark vs NSub & Moq
  - [ ] A lot more benchmarks
- [x] Property Support
  - [ ] Property setter
- [ ] Static Abstract (not support but we should fake an implementation)
- [ ] More matchers
  - [ ] Probably need a static matcher register :(
- [x] Compiled Setup calls
  - [x] Intercept Setup calls
  - [x] Support Base MatcherAttribute
  - [ ] Support Matcher arguments and fallback if impossible
  - [x] Special case AnyMatcher
- [x] Multiple uses of single type
- [ ] Protected classes
- [ ] Analyze all the things
- [x] Constructor support
  - [ ] All Create() to take constructor arguments & analyze them for likely match
- [ ] Debugger story
  - [ ] We do people want to step into
  - [ ] What objects will they inspect and how can it be helpful
- [ ] Documentation
- [ ] Make Pretend Implementation castable to something you can get the Pretend out of
- [ ] Check how many times a call has been made
- [x] out parameters
- [x] Make our own delegate for matcher calls
- [ ] Multiple method overloads
- [ ] Special case known good "Fake" implementations
  - [ ] EphemeralDataProtectionProvider
  - [ ] TimeProvider
  - [ ] TestLogger
- [x] Maybe not even use expressions in Setup
- [ ] Obsolete public types that exist only for SourceGen
  - [ ] And move to Pretender.Internals namespace
  - [ ] pragma disable obsolete warnings
- [ ] Compiled Behaviors?
- [ ] More Behaviors
- [x] Methods with no args should use Array.Empty<object?>()
  - [x] Or should use collection initializers
  - [x] Am I sure we can't use ref struct for CallInfo?
- [ ] Use CancellationToken everywhere