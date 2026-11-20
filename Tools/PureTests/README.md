# Pure tests without Unity

`Isekai.Rules` and the Blondie packages it uses have no engine references, so the Pure tests
(`Assets/Game/Tests/Pure`) also run on plain .NET: about 15 seconds instead of minutes of Unity start-up,
and no Unity licence, editor or test slot needed. The Unity editor may stay open.

```
dotnet test Tools/PureTests/Isekai.Game.Tests.Pure.csproj
dotnet test Tools/PureTests/Isekai.Game.Tests.Pure.csproj --filter "FullyQualifiedName~SoulTests"
```

- Needs the .NET 9 SDK, and `Library/PackageCache` (open the project in Unity once).
- Sources are compiled straight from `Assets/Game/Rules`, `Assets/Game/Tests/Pure` and the packages'
  `Runtime` folders, so new files are picked up with nothing to edit here. Libraries build as
  .NET Standard 2.1 with C# 9, like Unity 6, so rules code that would not compile in Unity fails here too.
- A Blondie package that gains a dependency needs a `ProjectReference` in its `.csproj` here.
- Unity's EditMode run stays the final check: EditMode and PlayMode tests only run there.
