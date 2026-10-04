# Sovereign — Phases 0-5 (GDD v3.0)

A real-time economic simulation: fiscal policy, monetary policy, the yield curve,
international debt, trade, currency intervention and regulation, in one dashboard.

**Phase 0 and Phase 1 are done.** The shell is real and inspectable, and the economy
underneath it runs: GDP, inflation, unemployment, the yield curve, credit ratings,
the currency and the debt stock all tick weekly in pure C#. The UI does not read it
yet — that is Phase 4.

## Open it

1. Unity Hub → Add → this folder (**Unity 6000.5.7f1**).
2. Open `Assets/_Project/Scenes/SovereignMain.unity`.
3. The Scene view shows the whole world laid out: seven nations, six trade routes,
   overlay layers. The Game view shows the dashboard skeleton.

Menu items under **Sovereign**:

| Item | What it does |
|---|---|
| Build Phase 0 Project | Creates anything missing. Non-destructive — existing assets and the scene are left alone. |
| Rebuild Data Assets (overwrites hand-tuned values) | Stamps the GDD's values back over every ScriptableObject. Asks first. |
| Verify Phase 0 | 115 checks: hierarchy, prefab instances, UXML wiring, GameDatabase slots, map meshes, asset counts. |
| Run Economy Tests | 43 checks across seven simulated decades - see below. |
| Print Economy Report | Year-by-year indicators for three scenarios, for reading magnitudes. |
| Capture Map Preview | Renders the map camera to a PNG. |
| Run Play Mode Smoke Test | Enters Play mode and checks the dashboard actually MOVED. |

Headless verify:

    Unity.exe -batchmode -quit -projectPath . -executeMethod Sovereign.EditorTools.Phase0Check.Run

## What exists

| | Count | Where |
|---|---|---|
| Sector definitions | 7 | `Assets/_Project/Data/Sectors` |
| Nation definitions | 7 | `Assets/_Project/Data/Nations` |
| Tax definitions | 13 | `Assets/_Project/Data/Taxes` |
| Spending categories | 30 | `Assets/_Project/Data/Spending` |
| Event definitions | 23 | `Assets/_Project/Data/Events` |
| Parameter assets | 6 | `Assets/_Project/Data/Parameters` |
| Map prefabs | 12 | `Assets/_Project/Prefabs/Map` |
| UXML panels / templates | 9 / 9 | `Assets/_Project/UI/Documents`, `Templates` |
| Coastline rings / map meshes | 94 / 3 | `Assets/_Project/Data/Map` |

`GameDatabase` (under `--- DATA ---` in the scene) holds an Inspector reference to
every one of those assets. It is the only place anything looks up config — there is
no `Resources.Load` and no `GameObject.Find` in this project.

## The two rules this project runs on

**Section 3 — nothing gameplay-visible is generated at runtime.** The scene, the
prefabs, the panels and the data are real Editor assets. `ProjectBuilder` is an
authoring tool that runs once in the Editor and leaves files behind; it does not
execute at play time.

**Section 2 — the simulation layer is pure C#.** `Assets/_Project/Scripts/SimulationCore`
has an assembly definition with `noEngineReferences: true`, so the compiler refuses
any `UnityEngine` reference inside it. Configuration enters as plain structs at boot.

## Deviations from the GDD, and why

- **Built-in render pipeline, not URP.** URP 17 is not served by the public package
  registry for this editor version, and a dashboard game gains nothing from it. Add
  it later via Package Manager; map materials use `Unlit/Color` and convert cleanly.
- **Seven NationDefinition assets, not six.** Section 3.5 says six, Section 3.3 gives
  `Nation_Home` one too. The hierarchy wins — the home nation needs a definition to
  be drawn.
- **Government & Military has a GDP share of 0.** Section 6 lists it as a dash; the
  other six sum to 1.00 exactly.
- **Answers to Section 22 are baked in:** immigration kept with a wage/housing cost,
  child credit as a dollar amount with a deliberately small birth elasticity,
  infrastructure as a standalone gauge (six categories tracked, one aggregate drives
  the drag), veteran count visible, wages floating, population screen scrollable.

## Next

Phase 1 — the pure C# economy and bond system, reading its numbers from the assets
above. Scripts attach to the manager GameObjects that already exist in the scene.

## The world map

Real geography, not a diagram. `Assets/_Project/Data/Map/Coastlines.txt` holds 94
coastline rings simplified from **Natural Earth 1:110m land** (public domain), and
`ProjectBuilder` turns them into three saved mesh assets — filled land, coastline
outlines and a graticule — placed under `MapRoot`.

The projection is **Web Mercator** (`MapProjection.cs`), clamped to ±82° because
Mercator sends the poles to infinity. It was chosen for two reasons: it is what
every online map uses, so it reads as a real map, and it stretches high latitudes,
which makes the world roughly square instead of the 2:1 band an equirectangular
projection would give — a better fit for a dashboard column.

Nations sit at their real coordinates: the home nation is North America, Euroland
western Europe, Sino-Pacific east Asia, Petro-Gulf the Arabian peninsula, Emerging
South equatorial Africa, Island Finance a Caribbean haven, the Northern Alliance
Scandinavia.

**Sovereign → Capture Map Preview** renders the map camera to a PNG, which is the
only reliable way to check the map without trusting the Game view.

One thing to know: the map camera renders through a transparent hole in the
dashboard. `.map-column` and `.map-viewport` must both stay transparent — give
either one a background colour and the map vanishes behind it.

## The economy (Phase 1)

`SimulationCore` is pure C# in an assembly with `noEngineReferences: true`, so the
compiler enforces the GDD §2 boundary. `SimulationConfigBuilder` is the only place
ScriptableObject values become plain structs; AnimationCurves are sampled into float
arrays so their shape crosses without the Unity type.

`SimulationRunner` sits on the manager GameObject from Phase 0 and is the clock:
one real second per in-game week at 1x, with `OnWeekTick`, `OnQuarterTick`,
`OnYearTick`, `OnBondAuction` and `OnCreditRatingReview`. Policy changes queue and
land at the quarter boundary, never mid-quarter.

### Tests, and why they are worth running

`Sovereign → Run Economy Tests` runs seven scenarios over whole decades. The first
version passed 30/30 while the economy was quietly nonsense — the baseline paid off
a third of the national debt on its own, the currency slid 34% with nobody touching
it, and a six-year depression peaked at 7% unemployment. Every *direction* was
right, so every direction test passed.

The magnitude tests exist because of that. They check that nothing drifts without a
policy change, that a Volcker-scale squeeze produces a Volcker-scale recession, that
deflation does not feed on itself forever, and that recovery is possible. Between
them they caught six real model defects, including a money-growth term that was a
function of inflation itself, and a Phillips curve so symmetric that 20% unemployment
demanded −5% inflation.

Run `Print Economy Report` after any change to a coefficient. Directions are cheap
to satisfy; magnitudes are where an economic model actually lives.

## Pressing Play

The dashboard is live. `DashboardController` sits on `UIRoot`, queries the element
names already in `Dashboard.uxml`, and refreshes on every `OnWeekTick`. The KPI cards
are stamped from `KPICard.uxml`; the chart is a `LineChartComponent` drawn with
`Painter2D` into the container the UXML defines (GDD 3.7's one permitted exception).

Working: the clock and date, six live KPI cards, the chart with nine series tabs and
a threshold line, the yield curve and its inversion flag, rating and market
confidence, the speed buttons, and the drawer buttons opening and closing panels.

**Fiscal and Monetary drawers are playable.** Every tax and spending asset gets a
stepper stamped from `PolicyRow.uxml`; presses queue to the quarter boundary, the row
says `QUEUED from 21%`, and each tax previews what the queued rate would raise. The
monetary drawer has rate, QE, QT, reserves, foreign issuance share, forward guidance
(immediate, per GDD 12), maturity strategy, the foreign-currency toggle, the emergency
cut, a live yield curve, the maturity profile and who holds your debt.

Every drawer is live.

`Sovereign → Run Play Mode Smoke Test` enters Play mode headlessly, checks the
dashboard changed, clicks Fiscal, presses a real `+` button, confirms the change is
queued and NOT yet applied, waits for the quarter boundary and confirms it landed,
then opens Monetary and checks every list built. It exists because "the
component is attached" and "the screen shows the simulation" are different claims,
and this project has already shipped a gap between them once.

## Phase 2: people, politics, and losing

**Population** (`SimulationCore/Population`): youth, working age and retired, with
births, deaths, ageing and migration. Working age times participation is the labour
force; the economy's unemployment rate decides how many work; the seven sectors split
them by share and health; **headcount times wage is the taxable income pool**, which
income tax and payroll tax are now levied on. Labour force growth feeds potential
growth, so an ageing society slows and immigration lifts it (GDD 21). Veterans are
counted, and the Veterans Benefits line is the count times the cost per head.

**Approval** (`SimulationCore/Politics`): each class and faction scores the conditions
it cares about using the ApprovalWeights asset, calibrated at boot so the opening
approvals are exactly the designer's. Unrest builds in proportion to how far below
40% a class sits, and a revolt ends the run. Every tax and spending stepper previews
its approval effect by class before the change is queued.

**Game over**: the clock stops and a card says why and how long you lasted, with
"Start a new run" and "Look at the wreckage". `SimulationRunner` has an Inspector
menu item, *Debug: End Run With A Revolt*, to check that screen without earning it.

### What Phase 2 exposed

- **Real wages fell every year forever.** Wages passed through 45% of inflation and
  had no productivity growth. Harmless while revenue was a share of GDP; fatal once
  income tax was levied on actual wages - the tax base shrank and debt ran to 137%.
- **Wage stickiness set growth, not lag.** A quarterly equation applied an annual
  rate scaled by stickiness, so sticky sectors grew slower forever. Each sector now
  has a target wage path and stickiness only sets how fast it is followed.
- **One class below 40% for fifteen months ended the game,** because unrest built at
  a flat rate. It now scales with depth: 38% simmers for years, 15% boils in months.

Economy tests switch revolt off (`RevoltEnabled`), because they deliberately ruin
the country for a decade to measure the economics, and a government falling halfway
through would freeze the thing being measured.

## Phase 3: the world acts

**Events** (`SimulationCore/Events`): 23 event assets move from an early signal on
the ticker, to an advisor warning, to a strike - or arrive as a pure shock. They
never offer a menu: GDD 17's rule is that your policy settings *at the moment of the
strike* decide the damage. The relief fund softens disasters, health spending softens
pandemics (and a pandemic that hits an under-funded system scars potential growth for
good), defence spending softens war, bank capital rules soften financial crises.

- **Trigger conditions** live on each EventDefinition. A banking crisis needs capital
  requirements at 10% or below; a hyperinflation needs unanchored expectations and
  15% inflation; a debt crisis needs debt above 180% and a junk rating.
- **War against you** is decided by the spending gap: casualties from WarParameters'
  curve, equipment wearing down, infrastructure damage, veterans, a forced minimum
  defence budget, the rally and then the drain. Lose badly enough and it is occupation.
- **Sovereign default** ends the run: consecutive failed auctions during a debt crisis.
- **The dice are seeded** (`StartingConditions.randomSeed`, 0 = new each run), so a
  run replays exactly - which is what makes an event bug reproducible.
- **EventParameters** holds every number that turns "preparation at the moment" into
  damage, plus oil and trade recovery speeds and the safe-haven effect.

On screen: markers on the map under the overlay layers (the war zone pulses), the
alert bell counting live events and opening World Events, a live news ticker, the
World Events drawer (active, warnings, war, nations, history) and the Advisor drawer
with Full / Data only / Silent. The advisor warns on the way INTO a condition -
inversion, watch negative, unanchored expectations, printing into inflation, debt
thresholds, crumbling infrastructure, falling approval, rising unrest - not weekly.

`Sovereign → Print Event Report` runs fifteen years on three seeds with the world
switched on. Expect about two events a year; a passive player drifts toward junk
debt, and an unlucky seed can bring the government down in year twelve.

### What Phase 3 exposed

- **Debt crises could never cause failed auctions.** Auction demand counted the risk
  premium as attraction, so a 260%-debt auction was 2.5x oversubscribed. Buyers now
  judge the risk-adjusted yield.
- **Every run opened with a false recession alarm.** The yield curve started at 0%
  and converged at different speeds, so the short end overtook the long for weeks.
  It now opens at equilibrium.

## Phase 5 (part one): nations that remember

**Geopolitics** (`SimulationCore/Geopolitics`): each of the six nations has a live
relationship that drifts toward what your policies have earned - your tariff on it,
trade agreements, sanctions, swap lines, aid - plus its temperament (Euroland wants
your environmental standards up; Sino-Pacific resents a weak currency undercutting
its exporters). Every quarter each nation acts on the thresholds in its own
NationDefinition (GDD 15):

- **Retaliation.** Tariff a nation past its threshold and it matches you. Walk out of
  a trade agreement and the damage is permanent.
- **The bond weapon.** Below -40 a creditor quietly stops buying at your auctions -
  weaker demand, yields drifting up. Below -70 it dumps your bonds.
- **Sanctions.** Below their line nations sanction you back. Sanction Petro-Gulf and
  it cuts oil supply; the Northern Alliance joins your coalitions.
- **Signature moves.** Sino-Pacific devalues every few years and calls a big surplus,
  a selling intervention or a large export subsidy manipulation. Emerging South asks
  to restructure - your aid decides whether that is a handshake or a default.
- **Capital flight.** Above 25% corporate tax, the corporate and capital gains base
  leaks to Island Finance.
- **Swap lines** only with friends; they count as reserves when defending the currency.

All the shared numbers are in the **GeopoliticsParameters** asset.

**Trade & Currency drawer**: global tariff, export subsidy, a tariff stepper per
nation (each says whether they will retaliate), a DiplomacyRow per nation
(Agreement / Sanction / Swap line toggles, aid share, live relationship and status),
currency intervention, reserves, FDI, competitiveness and capital flight.
**Regulatory drawer**: bank capital (rule out banking crises above 10%),
environmental and labour regulation, sector subsidies. **NationView** colours each
nation's ring on the map by relationship and shows its live status.

### What this part exposed

- **A creditor walking away barely moved yields.** Auction weakness only mattered
  once cover fell below 1.0x. The risk premium now responds continuously below a
  comfortable cover, so a lost buyer shows as a drift, as GDD 9.3 describes.
- **The bond weapon blunted itself.** Appetite was weighted by current holdings, so
  the more a creditor dumped, the less its absence counted. It is weighted by what
  each creditor normally buys.
- **The test suite was not deterministic.** Events spawned by hand rolled from
  clock-seeded dice, so one test had been passing by luck. Every test run now uses a
  fixed seed; the suite has been run twice back to back to confirm.

## Phase 5 (part two): ships, news, saves, achievements, Steam

- **Ships on the trade routes.** Each route is a Catmull-Rom curve through its
  `Waypoint_*` children; `TradeShip` prefab instances sail it. Ship count follows
  that nation's share of trade and the trade volume index; sanctions empty the lane.
  Ships stop when the clock is paused.
- **Headline generator.** 223 templates across 36 topics in
  `Data/News/Headlines.txt` (a TextAsset — edit it, no code). Topics and templates
  rest between uses so the ticker does not repeat itself.
- **Chart hover.** Moving over the dashboard chart shows that week's value and date;
  leaving restores the live readout.
- **Save / load.** SAVE and LOAD sit in the header. The whole run — economy, policy,
  the queued changes, open events, war, nations, headlines and both dice — is written
  as JSON to `Application.persistentDataPath/sovereign_save.json`, with a yearly
  autosave beside it. A loaded game opens paused. Saves carry a version, and one this
  build cannot read is refused before it touches the running game; a failed load
  puts back the game you were playing. The test plays a seeded world three years,
  saves, plays two more, then loads and replays the same two years: **the two
  futures match bit for bit**, compared as whole saves.
- **Achievements.** Soft Landing, Debt Hawk, Volcker Moment, Crisis Manager, Reserve
  Currency Defender, Trade War Veteran, The People's Champion and Unanchored, tracked
  in pure C# from the finished week, saved with the game, and announced on the
  ticker. Thresholds and titles live in `SO_AchievementParameters`; the API names
  are fixed in `AchievementIds` because Steam keys on them.
- **Steam.** `SteamManager` (on the Phase 0 `SteamManager` object) forwards
  unlocks and sets rich presence — "Year 2031 — Rating: AA — Approval: 67%" — through
  an `ISteamBackend`. By default it runs an offline backend that records the calls.
  To go live: add the Facepunch.Steamworks package, set your App ID on
  SteamManager (480, Valve's Spacewar test app, until you have one), create the eight
  achievements in the Steamworks dashboard with the API names in `AchievementIds`,
  and add `SOVEREIGN_STEAM` to Scripting Define Symbols. Steam Cloud can sync the save
  folder with auto-cloud; no code is needed.

### What this part exposed

- **The simulator kept state outside the state.** Event baselines, the advisor's
  memory, failed auctions and the headline rotation lived on the simulator objects,
  so a save would have silently lost them. They moved into `EconomyState`, which is
  why the save/load test compares entire saves: a field left behind shows up as a
  diverged future.
- **The smoke test hovered the chart through an open drawer.** The Regulatory drawer
  covers the data column, so the hover went to a policy row. That is correct
  behaviour; the test now closes the drawer first and asserts the chart is what the
  pointer hits.

Verified: Phase 0 115/115, economy 141/141, the play-mode smoke test passes through
ships, save/load, chart hover and Steam presence.

## Charts you can read, and the opening you actually play

**The charts have numbers.** Five values down the left in the series' own unit (% for
growth, inflation, debt and yields, M for population, $B for reserves, plain index
points otherwise) and game dates along the bottom. Hovering still shows the exact
value and quarter of any week. The scale is worked out when the data changes, so the
labels and the line always agree.

**The first sample is taken after boot, not during it.** `CreateState` used to record
week one before the population, budget or treasury existed, so every chart opened
with a zero that flattened its whole scale - the population chart was a vertical
cliff into a straight line. The opening sample is now taken at the end of `Prepare`.

**Openings are assets.** `ScenarioDefinition` holds a whole starting position, and
`SimulationRunner` boots from one. Two exist:

- **SO_Scenario_Aftermath** — what the game starts from. The war ended four months
  ago. GDP is 18.4T against a capacity of 21T, unemployment 15.5%, inflation 9.4%,
  infrastructure 29/100, and the reconstruction loan has just cleared: debt 135% of
  GDP, over half of it foreign, at a 4.2% coupon, with debt service alone running
  near 6% of GDP. The Northern Alliance - the nation you fought - opens at -68
  relationship, sanctioning you and refusing your bonds. Four briefing lines hit the
  ticker on the first morning.
- **SO_Scenario_Peacetime** — the calm GDD 18 dashboard position. Every economy test
  and every report boots this one, so the model is measured against a steady state
  instead of a recovery.

Two new mechanisms came out of building it:

- **Catch-up growth.** Output below potential now grows above trend, because idle
  plant and idle workers are cheap to put back to work. Without it the war's hole
  never closed: unemployment recovered while output stayed 14% below capacity
  forever.
- **...that tight money switches off.** The first version of catch-up quietly undid
  recessions the player had deliberately caused - five years at an 11% policy rate
  stopped biting. Catch-up now fades out as the real rate rises above neutral and is
  gone entirely 5 points above it, which is what a central bank holding rates high is
  actually doing.

Played out over ten years from the aftermath, with the test suite watching:

| | output gap | unemployment | debt/GDP | infrastructure |
|---|---|---|---|---|
| opening | -12% | 15.5% | 135% | 29 |
| hands off | -33% | 9.2% | 522% | 6 |
| rebuild and cut rates | -3% | 4.1% | 222% | 100 |

Verified: Phase 0 115/115, economy 160/160, play-mode smoke passes including the
chart's value scale and dates.

## Bond buybacks

The Monetary & Bonds drawer has a buyback desk. You commit cash, and at the quarter
the debt office buys your own bonds back at the market's price.

The price is the point. High yields against a low coupon mean your debt trades at a
discount, so a dollar retires more than a dollar of face. The quote is shown before
you commit - "74c buys $269B face, debt down $69B, interest +$7B/yr" - because both
halves are true at once: the cash is raised by issuing NEW debt at today's yields to
retire OLD debt paying the old coupon, so the principal shrinks while the interest
bill can rise. Retiring 5.7% debt with 12% money is a trade, not a windfall.

It is self-limiting in three ways the model enforces:

- **You are the buyer.** A larger order lifts the price against you, so buying the
  whole stock at once costs far more per dollar of face than buying steadily.
- **One quarter retires at most a fifth of the stock** - a debt office cannot clear
  the market in a week.
- **Above par it goes the other way.** If your bonds pay more than the market asks,
  buying them back costs more than it retires, and the ticker says so.

Half of what you buy comes out of foreign hands, so a sustained buyback pulls the
foreign holding share down - which in this build also means pulling your bonds back
from creditors who have used them as leverage.

Verified: economy 175/175 (including a buyback measured against the same quarter
played without one), Phase 0 115/115, and the play-mode smoke test presses the BUY
BACK button and watches the operation execute at the quarter.

## Two ways to read the dashboard

The chart panel has a view toggle at the front of its tab row.

- **SINGLE CHART** - the line chart, one series at a time, with a value scale, game
  dates and hover. For "what has this been doing?".
- **ALL METERS** - twelve vertical meters side by side, like a mixing desk, so the
  whole country reads in one look: growth, inflation, unemployment, debt, budget, the
  30-year yield, currency, trade balance, approval, unrest, infrastructure and the
  real wage. For "what shape are we in?", which is the question a crisis actually asks.

Each column carries its value on top, its name and the change over the last year
underneath, a tick across the track at the reading it is judged against, and a
colour: green inside tolerance, amber within twice it, red beyond. How each bar is scaled and judged is authored in `SO_MeterParameters` - the
series it reads, the ends of its scale, its target, its tolerance, and whether it is
judged by distance from target (inflation, currency) or by which side of it the
reading sits (unemployment, approval).

The smoke test presses the toggle, checks all twelve meters read a value, that they
are scaled to their own ranges rather than all alike, that the line chart is hidden
underneath, that the twelve columns share the width of the box instead of running off
it, and that the chart comes back when the view is toggled again.

Verified: Phase 0 115/115, economy 175/175, play-mode smoke passes through both views.

## Cities on the map

Every country now grows a skyline under its flag, so the map says what an economy is
MADE of rather than only what its numbers are.

- **Housing follows population.** Cottages appear as people do, and upgrade: past 340
  million they become apartment blocks, past 420 million towers.
- **One silhouette per sector**, so they are told apart at a glance: a sawtooth works
  with a chimney (Manufacturing), a barn, silo and furrows (Agriculture), a slim lit
  tower (Technology), a waisted cooling tower with a plume (Energy), a striped awning
  shopfront (Services & Retail), a columned bank under a pediment (Finance), a domed
  capitol with a flag (Government & Military). Each is coloured by its sector's own
  chart colour.
- **Count follows output, size follows health.** A sector's buildings are worth 6% of
  GDP each; a sick sector loses some and the survivors shrink, so decline reads as
  decline rather than as sudden demolition.
- **Foreign countries show what they are known for** - derricks in Petro-Gulf, works
  in Sino-Pacific, banks in Island Finance, farms in Emerging South - scaled by how
  much of your trade runs through them, and halved while they are sanctioning you.
- Buildings rise and fall over about half a second, on a jittered grid that is
  deterministic per country, so the same economy always draws the same town.

The shapes are procedural flat polygons built by `ProjectBuilder.Buildings.cs`, the
thresholds live in `SO_CityscapeParameters`, and `CityscapeView` on each Nation_*
object reads the week's state and raises or drops buildings to match.

### What this exposed

- **Back-face culling ate the whole feature.** The first build placed 32 buildings
  with correct transforms and correct renderer bounds, and drew nothing: flat polygons
  wound the wrong way against an `Unlit/Color` material that culls back faces. Flat
  shapes now carry both windings, and the smoke test checks the triangles FACE the
  camera rather than trusting bounds - bounds were perfect while the map was empty.
- **The nation flags sat where the cities wanted to be.** Marker squares were 1.1
  units with their name and status labels hanging below them, straight through the
  skyline. The flag shrank to 0.8 and its labels moved above it; everything below the
  marker now belongs to the city.

Verified: Phase 0 115/115, economy 175/175, and the smoke test asserts the home
country raises at least eight buildings of at least five kinds, that every foreign
nation builds too, and that all 97 building meshes face the camera.

## Cities across the country, and a map you can zoom

**Towns, not one pile.** Each nation carries authored `CitySite_*` children - five
for the home nation, three for everyone else - at real coordinates: a west coast, a
gulf, the industrial lakes, an east coast, the northern plains. A sector settles in
one town and spills into the next as it grows, while housing takes a plot in every
town, so a growing country fills up instead of stacking everything under its flag.
Sites are ordinary GameObjects in the scene: drag one in the Scene view and that
city moves, which is GDD 3.3's whole point.

A site that lands in the sea is walked inland in a widening spiral against the
coastline mesh before it is placed. Finding that needed a fix of its own: ear
clipping leaves zero-area slivers behind, every point is "inside" one of them, and
the first version of the test therefore declared the entire Pacific to be dry land.

**Zoom and pan.** `MapCameraController` on the map camera: the wheel zooms toward the
pointer between 2.5 and 21 (the whole world), right-drag pans with the point under the
cursor pinned, and the pan is clamped so the map cannot be lost off-screen. The map
column has **+**, **−** and **WORLD** buttons for the same thing. Input is only read
while the pointer is inside the camera's slice of the screen, so scrolling a policy
list never sails the camera across the Atlantic.

Verified: Phase 0 115/115, economy 175/175, and the smoke test presses the zoom
buttons (21 → 15.1, camera follows), pushes the zoom twenty notches to prove it stops
at its limit, checks WORLD restores the whole-world view, and checks the home country
occupies at least three of its cities spanning five world units.

## The map shows the country, not a pin

- **The marker squares are gone.** A nation was a coloured square with a ring; it is
  now just its name above its cities, and the NAME carries the relationship in its
  colour - green for allies, red for rivals - with the status line underneath. What
  you look at is the country itself.
- **Nothing is built at sea, and the test proves it.** City sites are snapped to the
  coastline mesh, and every individual plot is tested too: first pulled back toward
  its own town, and if that fails - the town centre can be offshore on an island or a
  river mouth - searched outward in a widening ring for the nearest ground. The smoke
  test counts buildings standing in water across ALL seven nations and fails on one.
  It found sixteen on the first attempt, which is how the outward search came to exist.
- **Ships sail both ways.** A lane used to be a one-way conveyor; ships now run out to
  the partner, turn around and bring something back, half of them heading each way.
- **Ship count is the trade.** `NationState.tradeIndex` records what tariffs, trade
  agreements and sanctions have actually done to each lane, and the map draws ships
  from that nation's share of your trade times that index times the world trade
  volume. A tariff war visibly empties a lane; sanctions close it.

Verified: Phase 0 115/115, economy 175/175, and the smoke test asserts that ships are
sailing in BOTH directions (6 outbound, 8 homebound) and that lanes carry different
numbers of ships (busiest 5, quietest 0) rather than a fixed decoration.

## Bars that explain themselves, and a map you can walk

**Hover any meter and it tells you what it is.** The tooltip gives the reading, the
target it is judged against, and a plain-language explanation - what the indicator
measures, and what it means when it moves. "Share of people who want work and cannot
find it. Around 4.5% is full employment. High unemployment costs you benefits, tax
revenue and, quickly, approval." The text lives in `SO_MeterParameters` beside each
bar's scale, so rewording one is an Inspector edit.

**Arrow keys and WASD walk the map.** Speed is measured in screens per second rather
than world units, so one key press covers the same visible distance zoomed in on a
city as it does looking at the whole world, and the same clamp that bounds dragging
bounds the keyboard - the map cannot be walked off the edge of the world. Only the
map moves; the dashboard is untouched.

Verified: Phase 0 115/115, economy 175/175, and the smoke test checks all twelve
meters carry an explanation, sends a real hover and reads the tooltip back
("Unemp 13.5% (aiming at 4.5%)"), then pans the map east and north and walks it into
its limit to prove it stops.

## Farmland, and crises that stop shouting

- **Farmland.** Agriculture is fields first and buildings second: half of what the
  sector raises is worked land - furrows under a hedgerow, in the sector's own colour
  - and half is the barn and silo. Farming nations abroad draw fields too, so
  Emerging South reads as farmland rather than as one generic shed.
- **Crises are diamonds now, not billboards.** A war used to be a 1.8-unit red square
  with a caption the size of a country; wars, disasters, unrest and FDI are now small
  diamonds (0.2 to 0.5 units) with labels at a third of their old size, stacked
  tightly beside the country's name instead of sprawling across it. The map went from
  unreadable to a world with things happening on it.
- **Names are smaller too**, and everything below a country's name belongs to its
  cities, so labels and skylines no longer fight for the same space.

Verified: Phase 0 115/115, economy 175/175, smoke passes with zero buildings at sea.

## Money on the headline row, and buildings that introduce themselves

- **Two more KPI cards: REVENUE and SPENDING**, in dollars, with revenue's share of
  GDP under one and the debt-service bill under the other. Every other fiscal number
  on the screen - the deficit, debt/GDP, the balance - is made of those two, and they
  were the only ones the player could not see. Both go green when the budget is in
  surplus. Cards narrowed to 112px so eight fit the row, and the row still wraps.
- **Hovering a building on the map says what it is**: "Manufacturing - 12.0% of GDP,
  20.4M employed at 78k average, health 70 - in good shape." Housing reports the
  population and how many of them are out of work. Hit testing is by distance rather
  than colliders (flat meshes, a hundred of them, a cheap loop), and the reach follows
  the zoom so the pointer feels the same close up as it does across the world.

  It shipped doing nothing, and the test said it worked, because the test called the
  tooltip directly instead of hovering anything. `Camera.ScreenToWorldPoint` returns a
  point on the camera's NEAR PLANE - twenty units in front of the map - so every
  building was twenty units from the pointer and nothing was ever found. The lookup is
  flat now, and the smoke test drives `MapHoverProbe.HoverAt` from a real screen
  position over a real factory, then over open sea to check the tooltip clears.
- **Buildings scaled down** from 0.6 to 0.42 with tighter spacing, so a country reads
  as a country rather than as a pile of icons.

Verified: Phase 0 115/115, economy 175/175, smoke passes - and it now checks the row
carries a revenue or spending card, and that hovering a factory describes it.

## Where the money comes from, and what a rate is charged on

**A revenue breakdown in the Fiscal drawer.** One bar per revenue line, longest
first, scaled against the biggest, with the amount and its share of the total beside
it; lines under 2% are gathered into "Everything else" so the chart says something
instead of listing everything. Underneath: "$3,208B a year across 9 lines, 17.3% of
GDP". Totals tell you how much you have; this tells you which taxes are carrying the
budget, which is what you need before you start moving rates.

**Every rate now names its base.** A policy row used to say nothing at all until a
change was queued, so a tariff sat there as a bare percentage of an invisible number
- exactly the thing a player cannot reason about. Rows carry a resting line now:

- Tax rows: `on $12,400B -> $2,232B/yr`
- Tariff rows: `5% of $395B imports = $20B/yr,  relationship -10`, and at zero it
  still names the base, because that is the number you are deciding about.

`TreasuryModel` gained `LineBaseBillions`, `NationImportBaseBillions` and
`NationTariffRevenue` for it - the same base the model taxes, not a second estimate
that can drift from it.

Verified: Phase 0 115/115, economy 175/175, and the smoke test reads the breakdown
bars and their total, and checks a tariff row states what it is charged on.

## The front page answers "where does the money come from?"

The bond chart has left the dashboard - the whole yield curve is in the Monetary &
Bonds drawer anyway - and the revenue breakdown has taken its place, above a one-line
market read (rating, foreign holdings, market confidence). The Fiscal drawer shows
the same chart from the same code, so the headline and the desk can never disagree.

**And every control you can change now prices its own next step:**

| row | reads |
|---|---|
| Tax | `on $12,400B -> $2,232B/yr,  next step +$124B/yr` |
| Spending | `$412B/yr, 2.2% of GDP,  next step +$25B/yr` |
| Tariff | `5% of $395B imports = $20B/yr,  +1% = +$4B/yr,  relationship -10` |
| Policy rate | `prices $31,600B of debt, now costing $1,772B/yr;  each step +$79B/yr once it has rolled over` |

That line shows at rest, not only once something is queued, so you can see what a
change is worth before you make it rather than after.

Verified: Phase 0 115/115, economy 175/175, and the smoke test checks the dashboard
draws the breakdown with its total, that the bond panel is gone from the front page,
and that a tariff row prices its next point.

## The revenue chart says what each line is

Two things were wrong with it. The bars were labelled with internal keys - a player
looking at "Value Added" had to ask what that meant - and there was nothing to ask.
Now:

- **Bars carry the tax's real name**: "VAT / Sales Tax", "Payroll Tax", "Top Income
  Rate", "Tariffs by nation". The names come from the same TaxDefinition assets the
  Fiscal drawer's rows are built from.
- **Hovering a bar explains the tax**: what it is charged on, who ends up paying it,
  and the catch. "VAT, or sales tax: a tax on consumer spending, collected at each
  stage on the value a business adds and paid at the till. The biggest base you have,
  and regressive by construction - it takes the largest share from the people with
  the smallest margin."

All thirteen lines are written in `ProjectBuilder.Taxes.Text.cs` and stamped onto
each asset's `description`, so the text lives with the data and an Inspector edit
rewords it. The drawer draws its own tooltip, because a drawer is a separate
UIDocument and cannot paint into the dashboard's.

Verified: Phase 0 115/115, economy 175/175, and the smoke test hovers the top bar,
reads the explanation back, and fails if any bar is still labelled with a key.

## Every stepper prices itself

The trade desk was the last place holding out: the global import tariff and the
export subsidy sat there as bare numbers, and the currency intervention said what it
would do without saying what it would cost. All three now read like the rest:

- Global import tariff: `3% of $395B imports = $12B/yr,  next step +$4B/yr`
- Export subsidy: `costs $0B/yr, 0.0% of GDP,  next step +$10B/yr`
- Currency intervention: `spends $480B/yr from reserves of $95B - 0 quarters of ammunition`
- QE / QT: `central bank holds $1,200B, +$400B/yr,  next step +$100B/yr`

The smoke test now walks every row of the trade drawer and fails the run if any of
them shows no money at all, so a new control cannot quietly ship as a bare number.

Verified: Phase 0 115/115, economy 175/175, smoke passes.

## Why people are unhappy, and how each industry is doing

**Hover an approval bar and it tells you why.** The model has always scored each group
on a handful of specific conditions and then thrown the detail away, leaving a bar
that moved for reasons the player could not see. Now:

> **Poor (30%)  57%  -  worst: Unemployment 13.5%**
> x  Unemployment 13.5%  16/100
> -  Welfare spending at its opening level  50/100
> +  Inflation 6.2%  62/100
> -  Healthcare spending at its opening level  50/100

Worst first, each with the live reading, what it scores out of 100, and a mark for
good, middling or bad. The Poor watch unemployment, inflation, welfare, healthcare
and labour protections; the Middle watch real wages, jobs, housing costs, their own
income tax, education and confidence; the Wealthy watch corporate and capital gains
tax, growth, debt and regulation; the three factions have their own lists.

This needed a refactor worth naming: `ApprovalModel.Components` builds the list of
(condition, weight, score) once, and the approval score is now the weighted average
OF THAT LIST. The explanation cannot drift from the bar it is explaining, because
there is only one definition. The economy suite passed unchanged through the change,
which is what proves the scores are identical.

**The Sectors tab shows the sectors.** It used to plot business investment - one
number for the whole economy - under a name that promised seven. Pressing it now
swaps the chart for a list of industries: a health bar each, coloured green, amber or
red, with the sector's share of GDP beside it. Hovering one gives headcount, average
wage, the wage bill, export share, foreign competition and regulatory burden.

The chart, the meters and the industry list share one box and exactly one may be
open - which the smoke test caught immediately, when two of them at once pushed the
view toggle underneath a meter.

Verified: Phase 0 115/115, economy 175/175, and the smoke test hovers the Poor bar,
reads the explanation back, and counts seven industries behind the Sectors tab.

## Where it goes

The same chart, for the other half of the budget. Under "WHERE THE MONEY COMES FROM"
on the front page - and in the Fiscal drawer above the spending steppers - is
"WHERE IT GOES": one bar per spending line, longest first, in amber rather than
green, with the small lines folded into "Everything else".

Its total says whether the budget is funded:

> $5,201B a year across 9 lines, 28.0% of GDP,  $2,102B borrowed

Hovering a bar explains that line, the same as the tax side. All thirty spending
categories are written in `ProjectBuilder.Spending.Text.cs` and stamped onto their
assets:

> **Debt Service** - Interest on the national debt. You do not set this: the bond
> market does, through the yields you have earned. It is the first claim on every
> dollar you raise.

> **Public Health** - Clinics, surveillance, vaccination. The pandemic mitigant: its
> level AT THE MOMENT a pathogen arrives is what decides the damage, and raising it
> afterwards is too late.

`RevenueBreakdown` now draws either side, so both charts are the same code reading a
different dictionary, and neither can drift from the treasury that fills it.

Verified: Phase 0 115/115, economy 175/175, and the smoke test counts the spending
bars, checks the total says borrowed or surplus, and hovers the top bar for its
explanation.

**Bars that line up, and names you can read.** The name column was sized with
`min-width`, so a long line - "Social Security Retirement" - widened its own column
and pushed that row's bar out of line. Fixing the widths introduced a worse bug:
clipping the label to the row height sliced every name through the middle. The
columns are fixed widths with their own full line height now.

The smoke test measures both - the left edge of every bar, and the height of every
label box - and it taught a lesson of its own twice over. Measured one frame after
the drawer opened, the elements had no geometry yet and the checks passed on nothing;
measured against the drawer's SPENDING chart, they found nothing again, because that
section sits behind its own tab and a hidden element has no size. A geometry check
must now prove it measured something before it is allowed to pass.

**A total debt card.** Debt/GDP says how heavy the debt is; it does not say what you
owe. A ninth KPI card carries the stock itself - "$31,600B" - with the share held
abroad underneath, which is the part that can be used against you. Cards narrowed to
104px so nine fit the row.

**And a deficit card.** The tenth: this year's gap in dollars, with its share of GDP
underneath. It flips its own title to SURPLUS and turns green if you ever get there.
Debt is the pile; the deficit is what you are adding to it this year, and it is the
one of the two you can do something about this quarter.

## A way to win, and a reason it is hard

The run had only one ending: the government falls. Now it has two.

**Prosperity is a state of affairs that has to hold.** The aftermath scenario
authors what winning means - debt under 60% of GDP, unemployment under 5.5%,
infrastructure above 75, real wages above 110 against the opening, approval above
50% - and all of it has to be true at once for two straight years. Slip on any of
them and the clock starts again, which makes the last mile as hard as the first. The
victory card stops everything the way a revolt does, and then the clock keeps running:
nothing stops a country losing this again.

**And the economics were making it too easy.** Before this, maximum austerity - every
tax at its ceiling, every programme at zero - cleared a 172% debt in five years while
unemployment fell to 0.5% and approval rose. Three things were wrong:

- **Spending cuts cost nothing.** The government's contribution to demand was measured
  against a baseline frozen at boot, so gutting the state was worth about one point of
  growth. There is a fiscal multiplier now, measured against the budget the economy has
  got used to, which drifts to a new level over a year: austerity hurts while it
  happens and leaves a smaller state behind.
- **Tax bases did not run away.** Avoidance is convex, so a few points above the going
  rate costs almost nothing and doubling a rate moves the money somewhere else.
- **Taxes cost too little politically**, which is now steeper for every class.

The three paths, measured (`Sovereign -> Probe Debt Payoff`):

| | austerity | left alone | disciplined rebuild |
|---|---|---|---|
| year 1 | unemployment 13%, approval 50 | deficit 14% | deficit 7%, unemployment 11% |
| year 5 | **government fallen** | debt 234% | debt 146%, unemployment 5% |
| year 10 | - | **collapse** | debt 114%, first surplus |
| year 20 | - | - | debt cleared |

The economy suite plays the sound path and requires it to win - and to take at least
ten years - then plays a government that does nothing and requires that it never does.

### The afternoon this cost

Three "experiments" in a row changed nothing, because a ScriptableObject keeps
whatever value it was first serialised with: a field added later and tuned in code
never reaches the game. `BuildMacroParameters` now stamps every new field, and the
rule is in the comment there.

Verified: Phase 0 115/115, economy 180/180, play-mode smoke passes.

## Making the opening survivable

The aftermath was unwinnable in the way that matters: the interest bill ran away
whatever the player did. Two things were wrong, and both were the model rather than
the difficulty.

- **The reconstruction loan was short paper.** 22% of the stock rolled over every year,
  repricing 5.6% debt onto 15% yields, so the average rate climbed to 8.7% by year five
  no matter how well the country was governed. A bailout loan is long and fixed, and
  the scenario now says so: 8% within a year, 27% in one-to-five, 65% beyond. The
  average rate peaks at 7.2% instead, and comes back down.
- **The market could not see the player trying.** The risk premium read the LEVEL of
  debt, its rating and its auctions - nothing about direction - so a government paying
  its debt down watched yields sit still for years. The premium now prices the last
  year's change in debt/GDP, worth up to 3 points off for a falling path and 5 points
  on for a rising one.

Same disciplined policy, before and after:

| year | interest before | interest after | 5y yield before | after |
|---|---|---|---|---|
| 1 | $2,313B | $2,033B | 12.7% | 12.7% |
| 5 | $3,345B | $2,590B | 8.9% | 7.6% |
| 10 | $3,029B | $2,011B | 5.4% | 4.2% |
| 15 | $1,570B | $775B | - | 3.6% |

Prosperity now arrives in year 15 rather than 18. Neglect is still terminal - debt
453% and the average rate at 17.6% by year ten, with the government long gone - and
maximum austerity still falls inside two years.

**An eleventh KPI card: AVG DEBT RATE**, the blended rate on the stock, with what the
market charges today underneath it. The gap between those two numbers is the whole
fiscal problem: $31,648B at 5.60% against a market asking 15.3%, repricing a little
at a time as it matures.

Verified: Phase 0 115/115, economy 180/180, play-mode smoke passes.

## "The game is impossible"

It was not the economics. Under sound policy - taxes a quarter of the way to their
ceilings, the social contract intact, the loan spent on infrastructure, rates cut -
played against the LIVE world with wars, pandemics and oil shocks, six seeds out of
six survived thirty years and five of six reached prosperity, in years 16 to 29
(`Sovereign -> Probe Survival`).

What was impossible was doing it. Tripling one infrastructure line at $5B a click is
thirty presses; the whole winning strategy was several hundred, each landing a
quarter later. Three fixes:

- **Steppers hold and run.** Press for one step, hold and it repeats and accelerates,
  shift-click jumps ten at a time. The plain click still works on its own, which the
  smoke test insisted on - wiring the step to the pointer alone made every stepper
  dead to the keyboard.
- **The goal is on screen.** A line above the KPI cards says what winning this
  scenario takes and what is still missing: "GOAL - still missing: debt 135% needs
  60%, unemployment 15.5% needs 5.5%, infrastructure 29 needs 75". A victory
  condition nobody can see is not a victory condition.
- **The opening briefing says where to start**, because the first three years are
  counter-intuitive: spend on the grid while the deficit is at its worst, and the bond
  market pays you for a debt that is FALLING long before it is low.

Verified: Phase 0 115/115, economy 180/180, play-mode smoke passes.

## A lighter loan

The opening debt came down from 172% of GDP to **135%**, and the average coupon on it
from 5.6% to **4.2%**. Opening debt service falls from near 10% of GDP to 6% - $1,043B
against revenue of $3,128B - and the deficit opens at 7.0% instead of double digits.
The scenario test's floor moved with it: the opening still has to be a reconstruction
loan rather than a normal budget, now read as debt at or above 125%.

The worry was the repricing cliff. A 4.2% coupon against an 8% policy rate is a wider
gap than 5.6% was, and 8% of the stock rolls off every year onto market paper. It went
the other way, because a smaller debt is a safer debt: the average rate settles at
4.5% and stays there instead of climbing to 7.2%.

| | opening | year 1 | year 5 | year 10 | year 15 |
|---|---|---|---|---|---|
| debt $B | 24,840 | 25,213 | 23,487 | 12,851 | **0** |
| debt/GDP | 135% | 125% | 89% | 36% | 0% |
| average coupon | 4.20% | 4.55% | 4.62% | 4.10% | 3.82% |
| interest $B | 1,043 | 1,148 | 1,086 | 527 | 0 |

That is the disciplined rebuild - taxes up a quarter of their headroom, the social
contract intact, infrastructure tripled, rates cut to 2.5%. The loan is now clear
around year 15 rather than year 20. Neglect still ends the run: hands off, the debt
reaches 598% of GDP and the country defaults between years 10 and 15.

**What this moved, and what it exposed.** Across six seeds of the live world under the
same policy, 6 of 6 survived thirty years and 4 of 6 reached prosperity, at years 14,
14, 17 and 20. Every seed cleared its debt to zero. The two that survived without
winning were held up by unemployment at 7.4% and 6.5% against a 5.5% target - so the
binding constraint is no longer the debt at all, it is the labour market. The debt
problem is close to solved by sound policy; the hard part is now getting the last two
points of unemployment out of a scarred economy.

Verified: Phase 0 115/115, economy 180/180.

## What is moving the numbers

The dashboard could say what every figure WAS and never once say why. The chart
answered "what happened"; nothing answered "what did I do". The line chart now sits
behind a series tab, and the box it used to own opens on a table of drivers.

Three sections - growth, unemployment, revenue - each with its headline figure and
the three things moving it most, signed, coloured and explained:

```
REAL GDP GROWTH   +3.64%   heading to +3.8%, trend is 3.1%
   Interest rates     +0.96   money is cheap right now
   Consumer demand    -0.71   people have stopped spending
   Infrastructure     -0.64   29/100 - too poor to grow through
```

**The arithmetic is the explanation.** `targetGrowth` used to be one expression adding
seven terms. It now records each term as it is computed and SUMS THE RECORD - the same
discipline as `ApprovalModel.Components`, where the score weighs the driver list rather
than keeping a second copy of it. An explanation that is not the arithmetic itself
drifts from it eventually. The proof that nothing moved underneath: the economy suite
passed 180/180 unchanged.

Trend growth is held out of the ranking and shown beside the headline instead. It is
much the largest term and barely moves, so ranked it would own a place in the top
three forever while telling the player nothing they can act on.

**"Heading to" is the other half.** Growth approaches its target by 6% a week, and
monetary policy transmits three to four quarters late, so a player's decision and its
visible effect are the better part of a year apart - which reads as a dead control.
The target responds at once, so it is now on the line beside the reading. The smoke
test fails if it ever disappears.

**Revenue is decomposed differently, on purpose.** A level cannot be explained by its
own parts - the breakdown chart below already shows those - so the table reports what
CHANGED against a year ago, and whether it was the rate or the base that moved:
"You moved the rate from 24.0% to 28.0%, and the base answered" against "The rate has
not changed at 21.0%, so this is the base growing under it." Each line's take and rate
is recorded weekly (`rev:` and `rate:` series), so the section is empty for the first
year and says so.

Three failures worth keeping, all from the smoke test:

- **A default view that hides another view breaks the tests that used it.** Making the
  table the default left the chart `display: None`, and the chart-hover stage went on
  picking at it.
- **Pressing the tab was not enough.** An element that was `display: None` has no
  geometry until the panel lays out again, and a thing with no geometry cannot be
  picked. The stage now presses, yields, and judges on the next tick.
- **`height: auto` let the table grow over its own tab row**, so the GDP tab could not
  be clicked. It is pinned to the chart's 200px with the rows sized to fit.

And one caught by reading the test's own output rather than its verdict: colouring a
driver by the sign of its number put the entire unemployment section backwards, since
a driver pushing unemployment DOWN is helping. Green means helping, which is not the
same as positive.

Verified: Phase 0 115/115, economy 180/180, play-mode smoke passes.

### Saying less

The first version of the table was unreadable in the way a textbook is unreadable.
Names carried their readings - "Spare capacity 12.3% below potential", "Pull toward
the natural rate 4.5%" - which overflowed a 150px column and hid the sign of the
number beside it. Notes explained the MECHANISM: "Okun's law: an economy growing
faster than its trend takes people on, and one growing slower lets them go."

What a player needs is what is happening now.

| | before | after |
|---|---|---|
| name | `Growth 3.6% against potential 3.1%` | `The economy` |
| note | `Okun's law: an economy growing faster...` | `growing fast enough to take people on` |
| value | `-4.50 pts/yr` | `-4.50`, with "falling 4.8 a year" in the heading |

Notes now flip with the state - "people are spending" against "people have stopped
spending", "exports are beating imports" against "imports are beating exports" - so
the line is a reading rather than a definition. Units live in the section heading
instead of on every row, where "pts/yr" was jargon three times over. The one reading
kept inside a note is infrastructure's, because that number is the player's to act on:
"29/100 - too poor to grow through".

Verified: economy 180/180, play-mode smoke passes.

## Making the player's decisions matter

Hovering unemployment showed this, and it was damning:

```
UNEMPLOYMENT      14.0%   falling 4.2 a year at this rate
   Finding work   -3.99   people and jobs keep pairing up by themselves
   The economy    -0.23   growing fast enough to take people on
```

The force the player controls was one seventeenth of the force they did not. A
government could watch unemployment fall for a decade and learn nothing about its own
policy. The driver table did not cause that - it revealed it.

Eight coefficients moved. Okun 0.5 to 0.9 so jobs follow growth; the monetary lag from
four quarters to two; growth's approach to its target from 0.06 a week to 0.10 (and
from a literal buried in UpdateDemand to a parameter); the fiscal multiplier 0.5 to
0.8; rate, consumption-tax and corporate-tax sensitivities all up by a quarter or more.

Three things went wrong, and each was found by measuring rather than reasoning.

**Cutting the automatic healing flat lost 6 of 6 runs to revolt.** That reversion was
not a clock, it was the damping on a feedback loop: unemployment feeds confidence,
confidence feeds demand, demand feeds growth, growth feeds unemployment. At Okun 0.9
the loop has enough gain to run away, and one shock finished a country. The pull is now
proportional to the DEPTH of the hole - full speed nine points above the floor, almost
nothing near it - so a shattered labour market still recovers on its own while the last
stretch belongs to the player.

**Two passes were aimed at unemployment on an assumption nobody had checked.** The
probe reported debt, unemployment and approval but not which victory condition was
actually missing. It does now, and it said: unemployment, in all five surviving runs.

**And the natural rate was the real problem.** Victory asks for 5.5% against a floor of
4.5% that no decision could move, so the last point had to come from growth alone while
shocks pushed back - a competent government won two runs in six. The floor is not a law
of nature: it is how well matched people are to the work there is. Schooling moves it
now, against what the country spent the day the run opened.

The first version of that made it a win button. Linear returns plus a probe compounding
the education budget 8% a year drove the floor to its minimum and finished at **0.7%
unemployment**, which no economy has ever run at - and nothing objected, because the
wage curve, the confidence term and the labour gap were all still reading the authored
4.5% rather than the floor the player had earned. The brake was pointing at the wrong
number. Three fixes: diminishing returns (the square root, so doubling the budget is
worth 0.6 points and quadrupling 1.5), a three-year lag so it is an investment a
government makes for its successor, and every reader of the natural rate pointed at the
earned one.

| | before | after |
|---|---|---|
| player's share of unemployment movement at 14% | ~5% | ~37% |
| below 7.5% unemployment | the clock dominates | the player dominates |
| levers on unemployment | growth, against a fixed floor | growth, and the floor itself |
| survived / won, 6 seeds | 6/6, 4/6 | 6/6, 5/6 |

The survival probe's player also had to grow up. It set policy once and left it for
thirty years, so the balance was being tuned to suit a government that never reacts to
anything; it now governs once a year - spends into unemployment when the books allow,
raises taxes when debt turns, and follows inflation with the policy rate.

Two ceilings in the deflation stress tests were raised, from 3.2x to 5.5x and 8x to
12x. A decade held at a 9% policy rate is meant to be a catastrophe, and with the rate
channel deliberately stronger and its lag halved the same decade now ends at 4.8x
rather than 3.1x. What those tests guard - finiteness, and deflation that does not
accelerate away - still passes, and the reason the number moved is recorded beside it.

Verified: Phase 0 115/115, economy 180/180, play-mode smoke passes.
