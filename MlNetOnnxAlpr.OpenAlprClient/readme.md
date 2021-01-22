https://stackoverflow.com/questions/5123655/msbuild-copy-whole-folder

  <ItemGroup>
    <_CopyItems Include="$(SolutionDir)\MlNetOnnxAlpr.OpenAlprClient\Dependencies\**\*.*">
      <InProject>false</InProject>
    </_CopyItems>
  </ItemGroup>
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Copy SourceFiles="@(_CopyItems)" DestinationFiles="@(_CopyItems->'$(OutDir)\%(RecursiveDir)%(Filename)%(Extension)')" SkipUnchangedFiles="true" OverwriteReadOnlyFiles="true" Retries="3" RetryDelayMilliseconds="300" />
  </Target>