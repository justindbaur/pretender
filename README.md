# Pretender

[![NuGet](https://img.shields.io/nuget/v/Pretender)](https://www.nuget.org/packages/Pretender)


## Example

```c#
var pretendMyInterface = Pretend.That<IMyInterface>();

pretendMyInterface
    .Setup(i => i.MyMethod(It.IsAny<string>(), 14))
    .Returns("Hello!");

var myInterface = pretendMyInterface.Create();
```

## Benchmarks vs Competitors

> Doing a simple `Is.IsAny<string>()` or equivalent call.

| Method          | Mean         | Error      | StdDev       | Median       | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |-------------:|-----------:|-------------:|-------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| MoqTest         | 45,863.83 ns | 897.457 ns | 1,641.051 ns | 45,172.41 ns | 509.57 |   19.58 | 1.3428 | 1.2207 |    8850 B |       22.12 |
| NSubstituteTest |  4,151.76 ns |  82.517 ns |   126.012 ns |  4,133.53 ns |  45.69 |    1.68 | 1.2360 | 0.0381 |    7760 B |       19.40 |
| PretenderTest   |     91.08 ns |   1.432 ns |     1.270 ns |     91.33 ns |   1.00 |    0.00 | 0.0637 |      - |     400 B |        1.00 |

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
