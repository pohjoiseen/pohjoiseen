// adapted from https://stackoverflow.com/a/62879437

const childProcess = require("child_process");
const fs = require("fs");

function writeToEnv(key, value) {
    const empty = !key;

    if (empty) {
        fs.writeFileSync(".env.local", "");
    } else {
        fs.appendFileSync(".env.local", `${key}='${value.trim()}'\n`);
    }
}

// reset .env file
writeToEnv();

const packageJson = fs.readFileSync('package.json', 'utf-8');
const pkg = JSON.parse(packageJson);
writeToEnv("REACT_APP_VERSION", pkg.version);

childProcess.exec("git show --no-patch --format=%cs HEAD", (err, stdout) => {
    writeToEnv("REACT_APP_GIT_DATE", stdout);
});
childProcess.exec("git rev-parse --short HEAD", (err, stdout) => {
    writeToEnv("REACT_APP_GIT_SHA", stdout);
});
