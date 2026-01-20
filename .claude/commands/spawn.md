@include shared/constants.yml#Process_Symbols

@include shared/command-templates.yml#Universal_Flags

Spawn specialized sub-agent→handle specific task in $ARGUMENTS.

Thinking flags (optional):
- --think→multi-agent coordination planning
- --think-hard→complex agent workflow design
- --ultrathink→enterprise-scale agent orchestration

Examples:
- `/user:spawn --task "optimize database queries" --think` - Spawn data agent w/ coordination analysis
- `/user:spawn --task "implement auth system" --think-hard` - Complex security impl w/ planning
- `/user:spawn --task "migrate to microservices" --ultrathink` - Enterprise arch w/ full orchestration

--task flag:
- Define clear, focused assignment | Provide necessary context & constraints
- Set specific success criteria | Establish integration points

Agent specializations:

Frontend Agent:
- UI/UX impl | Component dev
- State management
- Performance optimization
- Accessibility compliance

Backend Agent:
- API development
- Database operations
- Business logic implementation
- Integration services
- Performance tuning

DevOps Agent:
- CI/CD pipeline setup
- Infrastructure automation
- Deployment strategies
- Monitoring configuration
- Security hardening

Data Agent:
- Data analysis and transformation
- Database optimization
- ETL pipeline development
- Data quality assurance
- Reporting solutions

Best practices for spawning agents:

1. Define Clear Scope
   - Specific deliverables
   - Clear boundaries
   - Time constraints
   - Quality expectations

2. Provide Context
   - Relevant code sections
   - Documentation links
   - Previous decisions
   - Technical constraints

3. Set Success Criteria
   - Measurable outcomes
   - Quality standards
   - Integration requirements
   - Testing expectations

4. Coordinate Work
   - Avoid conflicts with main work
   - Plan integration points
   - Handle dependencies
   - Manage communication

Integration workflow:
1. Spawn agent with clear task definition
2. Agent works independently on task
3. Agent provides progress updates
4. Results reviewed and integrated
5. Knowledge transferred to main context

## Benefits & Integration

Benefits:
- Parallel task execution
- Specialized expertise
- Focused context
- Reduced cognitive load
- Faster completion

Research requirements for agent spawning:
- Task specialization → Research domain-specific best practices and patterns
- Agent coordination → WebSearch for multi-agent workflow patterns
- Integration strategies → C7 documentation for framework-specific integration methods
- Quality assurance → Must verify agent output validation patterns
- Never spawn without clear scope - always research coordination patterns
- All agent instructions must cite sources: // Source: [coordination guide reference]

Report Output:
- Agent coordination logs: `.claudedocs/reports/agent-spawn-<timestamp>.md`
- Task completion summaries: `.claudedocs/summaries/agent-results-<timestamp>.md`
- Ensure directory exists: `mkdir -p .claudedocs/reports/ .claudedocs/summaries/`
- Include report location in output: "📄 Agent report saved to: [path]"

Deliverables: Completed task results from spawned agent, integration plan for merging work, knowledge transfer documentation, coordination summary, and quality validation report.