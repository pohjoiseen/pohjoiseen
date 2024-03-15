// adapted from https://stackoverflow.com/a/62879437
// cannot do it right in package.json because I normally build the project on Windows

const childProcess = require("child_process");
const fs = require("fs");

function writeToEnv(key, value) {
    const empty = !key;

    if (empty) {
        fs.writeFileSync(".env", "");
    } else {
        fs.appendFileSync(".env", `${key}='${value.trim()}'\n`);
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
