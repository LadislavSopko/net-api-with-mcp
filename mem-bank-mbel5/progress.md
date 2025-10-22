§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[COMPLETE]
✓infrastructure::CPM{Directory.Packages.props}
✓infrastructure::BuildProps{Directory.Build.props+Version.props}
✓infrastructure::SDK{global.json§9.0.300}
✓infrastructure::Git{.gitignore}
✓structure::Folders{src/+tests/+tasks/+mem-bank-mbel5/+3rdp/}
✓memory::MemoryBank{5cores:created}
✓create::net-api-with-mcp.slnx
✓update::Directory.Packages.props{MCP+xunit+FluentAssertions+Serilog}
✓scaffold::McpPoc.Api{csproj+Program.cs+launchSettings}
✓implement::Models{User.cs}
✓implement::Services{IUserService+UserService}
✓implement::Controllers{UsersController+MCPAttributes}
✓implement::Extensions{McpServerBuilderExtensions:170lines}
✓implement::ActionResultUnwrapper{custom-marshaller}
✓build::Project{dotnet-build:success}
✓test::TestProject{McpPoc.Api.Tests+WebApplicationFactory}
✓test::MCPClient{ModelContextProtocol.SDK:HttpClientTransport}
✓test::Discovery{5/5:pass→3tools+snake_case}✓
✓test::HTTP{3/3:pass→coexistence-proven}✓
✓test::Invocation{5/5:pass→ActionResult-unwrapped}✓
✓test::Serialization{2/2:pass→diagnostics}✓
✓solution::Complete{15/15:100%}✓

[ACTIVE]
¬none::POCFinished

[BLOCKED]
¬none::AllClear

[TODO]
?cleanup::Filters/ActionResultUnwrapperFilter.cs{obsolete}
?document::Commit{findings+solution}

[METRICS]
@files::Created{#25:src+tests+tasks+extensions}
@tests::Status{15/15:pass→100%}✓
@confidence::POC{100%:hypothesis-fully-proven}✓
@time::Total{~4h:full-solution-with-debugging}
@linesOfCode::~750{api+tests+extensions}

[KNOWN_ISSUES]
✓resolved::MCPSDKBehavior{YES:discovers-controllers+snake_case}
✓resolved::DISupport{YES:injects-services}
✓resolved::ActionResultHandling{YES:custom-marshaller-works}
✓resolved::ToolNaming{YES:explicit-snake_case-conversion}
✓resolved::Serialization{YES:unwrap-before-serialize}

[SUCCESS_TRACKING]
@discovery::SUCCESS{3tools:get_by_id+get_all+create}✓
@invocation::SUCCESS{all-tools-return-data}✓
@coexistence::SUCCESS{HTTP+MCP:both-work}✓
@completion::100%{poc-fully-complete}✓

[POC_VERDICT]
✅Controllers→MCPTools{YES:feasible}
✅DirectAttributes{YES:[McpServerToolType]+[McpServerTool]:works}
✅DependencyInjection{YES:services-injected-properly}
✅Coexistence{YES:HTTP+MCP:no-conflicts}
✅SelectiveExposure{YES:¬[McpServerTool]:not-exposed}
⚠️CustomMarshaller{REQUIRED:ActionResult<T>:needs-unwrapping}
✅Solution{EXISTS:WithToolsFromAssemblyUnwrappingActionResult}

[DEBUGGING_JOURNEY]
>attempted::CallToolFilter{post-marshalling→failed:too-late}
>discovered::ValueTaskProblem{result:null-after-await}!
>debugged::ReflectionAIFunction{ReturnParameterMarshaller:arg2-valid}
>found::RootCause{new-ValueTask:loses-value}!
>implemented::CustomMarshaller{ValueTask.FromResult:fixes-it}
>added::ActionResultUnwrapping{extract-value:while-fixing-marshaller}
>resolved::TwoProblemsOneSolution{✓works:15/15-tests}
