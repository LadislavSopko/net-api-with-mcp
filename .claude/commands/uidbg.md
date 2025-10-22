@include shared/constants.yml#Process_Symbols

@include shared/command-templates.yml#Command_Header

Visual UI debugging using Playwright screenshots & LMT vision analysis for $ARGUMENTS.

@include shared/command-templates.yml#Universal_Flags
@see shared/thinking-modes.yml ∀ thinking flags
@see shared/mcp-flags.yml ∀ MCP controls

Examples:
- `/uidbg` - Analyze current page @ HD resolution for general issues
- `/uidbg --route /login --size mobile` - Analyze login page @ mobile size
- `/uidbg --element .data-table --focus alignment` - Analyze specific element
- `/uidbg --route /dashboard --width 1440 --height 900` - Custom dimensions

Default behavior (no args):
- Uses current browser page if open, else http://localhost:4200
- HD resolution (1920x1080)
- General UI analysis (layout, alignment, visual bugs)
- Model: devstral-vision-small-2507@q6_k (fast & accurate)

Flags:
**--route <path>** - Navigate to specific route (e.g., /dashboard, /clients)
**--size <preset>** - Resolution presets: HD (1920x1080), mobile (375x667), tablet (768x1024), laptop (1366x768)
**--width/--height** - Custom dimensions in pixels
**--element <selector>** - Screenshot specific element instead of full page
**--model <name>** - LMT model: q4_k_m (fastest), q6_k (default), q8_0 (best quality)
**--focus <aspect>** - Analysis focus: layout, alignment, colors, spacing, accessibility, responsive, all

Analysis aspects:
**layout:** Component positioning, visual hierarchy, overflow issues
**alignment:** Element alignment, spacing consistency, grid issues  
**colors:** Contrast, readability, theme consistency
**spacing:** Padding, margins, whitespace distribution
**accessibility:** ARIA labels, focus indicators, contrast ratios
**responsive:** Mobile/tablet rendering, breakpoint issues
**all:** Comprehensive analysis of all aspects

Workflow:
1. Check if browser is already open → use current URL
2. If not, navigate to specified route or default (localhost:4200)
3. Set viewport size based on flags or default to HD
4. Take screenshot (full page or element)
5. Send to LMT MCP server for analysis
6. Display visual issues found & suggestions

Report includes:
- Visual issues identified
- Severity levels (critical, high, medium, low)
- Specific elements affected
- Recommended fixes
- Accessibility concerns if detected