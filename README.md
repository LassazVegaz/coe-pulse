# COE Pulse

Check historical Singapore COE bidding data.

## Data source
This project uses the [COE dataset](https://data.gov.sg/datasets/d_69b3380ad7e51aff3a7dcc84eba52b8a/view) publicly available.

## Tech Stack
- backend:
  - Framework: .NET
  - Location: app\backend
  - Description: Download the dataset. Allow the frontend to query the dataset.
- frontend:
  - Framework: NextJS + TypeScript
  - Location: app\client
  - Description: Display COE data to users in an interactive way.
