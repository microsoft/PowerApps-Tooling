

## Creating new Pcf Control
- pac install latest
- mkdir MyLabel
- cd MyLabel
- pac pcf init --namespace PATooling.Samples --name MyLabel --template field --framework react --run-npm-install
- <modify the MyLabel code as desired>
- npm build
- In make.test, create a solution with publisher.
- pac pcf push --solution-unique-name PAToolingSamplePcfControls --incremental

