# Pretender

## To-do list

In no particular order

- [x] Intercept .Create calls
    - [x] Base Implementation name off of ITypeSymbol derived info
    - [ ] Should allow naming customization
    - [ ] Respect [Pretend] attribute that is placed on a partial class
- [x] Async/Task Support
    - [ ] Custom awaitable (honestly don't think I care)
- [ ] Classes (maybe already do)
- [ ] Benchmark vs NSub & Moq
- [ ] Property Support
- [ ] Static Abstract (not support but we should fake an implementation)
- [ ] More matchers
- [x] Compiled Setup calls
  - [x] Intercept Setup calls
  - [x] Support Base MatcherAttribute
  - [ ] Support Matcher arguments
- [x] Multiple uses of single type
- [ ] Protected classes
- [ ] Analyze all the things
- [ ] Constructor support
- [ ] Debugger support
- [ ] Documentation
- [ ] Make Pretend Implementation castable to something you can get the Pretend out of
- [ ] Check how many times a call has been made
- [ ] out parameters
- [x] Make our own delegate for matcher calls
- [ ] Multiple method overloads
- [ ] Special case ILogger
- [ ] Special case known good "Fake" implementations
  - [ ] EphemeralDataProtectionProvider
  - [ ] TimeProvider
  - [ ] TestLogger
- [ ] Maybe not even use expressions in Setup
- [ ] Obsolete public types that exist only for SourceGen
  - [ ] pragma disable obsolete warnings
- [ ] Compiled Behaviors?
- [ ] More Behaviors
- [x] Methods with no args should use Array.Empty<object?>()
  - [x] Or should use collection initializers
  - [x] Am I sure we can't use ref struct for CallInfo?
- [ ] Use CancellationToken everywhere