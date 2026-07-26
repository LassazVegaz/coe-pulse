import path from "node:path";
import fs from "node:fs";

type ObjectWithUnknownProps = Record<string, unknown>;
type AnalyzedProps = Record<string, Set<string>>;

const FILE = path.join(process.cwd(), "NParksTracks.geojson");
const OUTPUT = path.join(process.cwd(), "analyzed.json");
const DEBUG = process.argv[2] === "-d";

const makePrintable = (data: Record<string, Set<string>>) => {
  const printable: Record<string, string[]> = {};

  for (const k in data) printable[k] = [...data[k]!];

  //   console.log(printable);

  return printable;
};

const getProps = (
  data: ObjectWithUnknownProps[] | ObjectWithUnknownProps,
  props: AnalyzedProps = {},
  ignore: Set<string> = new Set<string>(),
  debug: ((k: string, data: unknown) => void) | undefined = undefined,
) => {
  if (Array.isArray(data)) {
    for (const o of data) getProps(o, props, ignore);
  } else {
    for (const k in data) {
      debug?.(k, data[k]);

      if (ignore.has(k)) continue;

      if (!(k in props)) props[k] = new Set<string>();

      if (typeof data[k] === "string") props[k]!.add(data[k]);
      else if (data[k] === null) props[k]!.add("null");
      else props[k]!.add(typeof data[k]);
    }
  }

  return props;
};

const { features } = JSON.parse(fs.readFileSync(FILE, "utf8")) as {
  features: ObjectWithUnknownProps[];
};

const featuresProps = getProps(features);
const geometryProps: AnalyzedProps = {};
const propertiesProps: AnalyzedProps = {};

const propsToDebug = new Set([
  "TYPE",
  "AMA_CATEGORY",
  "ALLOW_WALKING",
  "ALLOW_CYCLING",
  "ALLOW_WHEELING",
  "ALLOW_PMD",
]);

for (const f of features) {
  const { geometry, properties } = f;

  getProps(geometry as ObjectWithUnknownProps, geometryProps);
  getProps(
    properties as ObjectWithUnknownProps,
    propertiesProps,
    new Set<string>(["INC_CRC"]),
    DEBUG
      ? (k, data) => {
          if (propsToDebug.has(k) && typeof data === "object")
            console.log("DEBUG:", k, "-", data);
        }
      : undefined,
  );
}

fs.writeFileSync(
  OUTPUT,
  JSON.stringify({
    features: makePrintable(featuresProps),
    geometry: makePrintable(geometryProps),
    properties: makePrintable(propertiesProps),
  }),
);
