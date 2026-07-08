import { useEffect, useMemo, useState } from 'react';
import {
  Activity,
  BarChart3,
  Building2,
  CheckCircle2,
  ClipboardList,
  CreditCard,
  FileText,
  Home,
  Lock,
  Network,
  Search,
  ShieldCheck,
  Workflow,
} from 'lucide-react';
import { getHealth, getModules, platformApi } from './api/platformApi';

type ApiState = 'loading' | 'online' | 'offline';

const moduleIcons: Record<string, typeof Activity> = {
  Billing: CreditCard,
  Claims: ClipboardList,
  Documents: FileText,
  Identity: Lock,
  Organizations: Building2,
  Properties: Home,
  Reporting: BarChart3,
  Workflow: Workflow,
};

const workflowItems = [
  'Intake claim package',
  'Enrich property profile',
  'Build estimate strategy',
  'Generate client-ready report',
];

export function App() {
  const [apiState, setApiState] = useState<ApiState>('loading');
  const [modules, setModules] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadPlatformStatus() {
      try {
        setApiState('loading');
        const [health, moduleNames] = await Promise.all([
          getHealth(controller.signal),
          getModules(controller.signal),
        ]);

        setApiState(health.status === 'healthy' ? 'online' : 'offline');
        setModules(moduleNames);
        setError(null);
      } catch (exception) {
        if (!controller.signal.aborted) {
          setApiState('offline');
          setError(exception instanceof Error ? exception.message : 'Unable to reach the API');
        }
      }
    }

    void loadPlatformStatus();

    return () => controller.abort();
  }, []);

  const statusLabel = useMemo(() => {
    if (apiState === 'loading') {
      return 'Checking API';
    }

    return apiState === 'online' ? 'API online' : 'API offline';
  }, [apiState]);

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand-mark">PI</div>
        <nav aria-label="Primary navigation">
          <a className="nav-item active" href="#overview">
            <Activity size={18} />
            Overview
          </a>
          <a className="nav-item" href="#properties">
            <Home size={18} />
            Properties
          </a>
          <a className="nav-item" href="#claims">
            <ClipboardList size={18} />
            Claims
          </a>
          <a className="nav-item" href="#documents">
            <FileText size={18} />
            Documents
          </a>
          <a className="nav-item" href="#reporting">
            <BarChart3 size={18} />
            Reporting
          </a>
        </nav>
      </aside>

      <section className="workspace" id="overview">
        <header className="topbar">
          <div>
            <p className="eyebrow">Property Intelligence Platform</p>
            <h1>Claims and property intelligence workspace</h1>
          </div>
          <div className={`api-pill ${apiState}`}>
            <span />
            {statusLabel}
          </div>
        </header>

        <section className="toolbar" aria-label="Workspace tools">
          <label className="search-box">
            <Search size={18} />
            <input placeholder="Search properties, claims, documents" />
          </label>
          <a className="button secondary" href={platformApi.swaggerUrl} target="_blank" rel="noreferrer">
            API Swagger
          </a>
          <button className="button primary">New Claim</button>
        </section>

        {error ? <p className="alert">{error}. Start the API on {platformApi.baseUrl}.</p> : null}

        <section className="metrics-grid" aria-label="Platform metrics">
          <article className="metric-card">
            <span>Open claims</span>
            <strong>128</strong>
            <small>24 require strategy review</small>
          </article>
          <article className="metric-card">
            <span>Properties monitored</span>
            <strong>3,842</strong>
            <small>91 updated this week</small>
          </article>
          <article className="metric-card">
            <span>Documents processed</span>
            <strong>12.6k</strong>
            <small>98.4% extraction confidence</small>
          </article>
          <article className="metric-card">
            <span>Subscription tier</span>
            <strong>Growth</strong>
            <small>Usage and billing ready</small>
          </article>
        </section>

        <section className="content-grid">
          <div className="panel wide">
            <div className="panel-heading">
              <div>
                <p className="eyebrow">Operational flow</p>
                <h2>Claim strategy pipeline</h2>
              </div>
              <ShieldCheck size={22} />
            </div>
            <div className="pipeline">
              {workflowItems.map((item, index) => (
                <div className="pipeline-step" key={item}>
                  <div className="step-index">{index + 1}</div>
                  <div>
                    <strong>{item}</strong>
                    <span>{index === 0 ? 'Ready for assignment' : 'Template workflow prepared'}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="panel">
            <div className="panel-heading">
              <div>
                <p className="eyebrow">Backend modules</p>
                <h2>API surface</h2>
              </div>
              <Network size={22} />
            </div>
            <div className="module-list">
              {(modules.length > 0 ? modules : Object.keys(moduleIcons)).map((moduleName) => {
                const Icon = moduleIcons[moduleName] ?? CheckCircle2;

                return (
                  <div className="module-row" key={moduleName}>
                    <Icon size={18} />
                    <span>{moduleName}</span>
                    <CheckCircle2 className="module-check" size={17} />
                  </div>
                );
              })}
            </div>
          </div>
        </section>
      </section>
    </main>
  );
}
