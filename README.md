# COE Pulse SG

COE Pulse is a one-page dashboard for exploring Singapore Certificate of
Entitlement (COE) bidding results. It presents premiums, quotas, bid demand,
success rates, historical trends, and the latest bidding exercise.

Data source: [COE Bidding Results](https://data.gov.sg/datasets/d_69b3380ad7e51aff3a7dcc84eba52b8a/view)
from data.gov.sg.

## Architecture

- `app/backend`: .NET 10 Web API that synchronizes and queries the CSV dataset.
- `app/client`: Next.js 16 dashboard with category and history filters.
- `terraform`: AWS ECR, ECS Fargate, Application Load Balancer, Secrets Manager,
  and CloudWatch Logs.
- `.github/workflows/deploy-backend.yml`: backend test, image build, and AWS
  deployment workflow.

The API checks the dataset metadata on startup. It downloads a new CSV only when
`lastUpdatedAt` changes, then replaces the local file atomically. This avoids an
unnecessary full download on every restart.

## Run locally

### 1. Backend

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
cd app/backend/COEPulse.API
dotnet user-secrets set API_KEY "<your-data.gov.sg-api-key>"
dotnet run
```

The launch profile serves the API locally. Check its printed URL, then verify:

```text
GET /health
GET /api/coe?pageNo=1&pageSize=100&fromYear=2022&categories=A&categories=B
```

Query parameters:

- `pageNo`: page number; defaults to 1.
- `pageSize`: 1–2,000; defaults to 100.
- `fromYear` / `toYear`: optional inclusive year range.
- `categories`: repeatable values `A` through `E`.

The response contains `records`, `total`, `pageNo`, and `pageSize`.

### 2. Client

Requirements: Node.js 20+ and pnpm.

Create `app/client/.env.local`:

```dotenv
BACKEND=http://localhost:5000
```

Use the actual backend URL printed by `dotnet run`, then:

```bash
cd app/client
pnpm install
pnpm dev
```

Open [http://localhost:3000](http://localhost:3000).

## Tests and checks

```bash
dotnet test app/backend/COEPulse.API.Tests/COEPulse.API.Tests.csproj

cd app/client
pnpm lint
pnpm build
```

The backend tests cover downloading a new dataset and skipping the download
when the remote metadata timestamp is unchanged.

## AWS deployment

The Terraform is deliberately small and uses the account's default VPC. The
workflow expects these GitHub Actions secrets:

- `AWS_ROLE_ARN`: IAM role trusted by GitHub's OIDC provider.
- `TF_STATE_BUCKET`: existing S3 bucket for Terraform state.
- `DATA_GOV_API_KEY`: data.gov.sg API key.

Run the **Deploy backend** workflow manually. It creates the ECR repository,
runs the tests, pushes an immutable image tagged with the commit SHA, and
applies the ECS infrastructure.

For a production system, add HTTPS with ACM, restrict CORS to the frontend
origin, use private subnets with a NAT/VPC endpoints, configure autoscaling, and
persist the downloaded dataset in S3 rather than task-local storage.

## Design decisions

- A server-side Next.js action keeps the backend URL out of browser code and
  avoids browser CORS coupling during normal use.
- The backend caches the complete dataset in memory because this public CSV is
  small; filtering is simple and fast without introducing a database.
- CSV replacement is atomic so a failed download cannot corrupt the active
  dataset.
- The dashboard uses lightweight SVG/CSS charts to keep the assessment small
  and avoid a charting dependency.

## Limitations

- The dataset does not check if a new dataset is available in data.gov.sg periodically. It only checks new data at the beginning of the application. This is a problem if the application keep running for more than 3 weeks. data.gov.sg updates data every 2-3 weeks time.
- This application can have more filters. Example: Selecting a date range. Displaying a pie chart of various data distributions.
- Downstream data from backend to frontend can be handled in more user friendly ways than implemented in this application.
