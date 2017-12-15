# minimalist-xrm-tools
Tools and other utilities for CRM Developers, Customizers and Administrators that I have personally written.

## Name
`wrt` - Web Resource Tool

### Synopsis:
```
      wrt --conn [--pause]
      wrt --create [--pause]
      wrt --deploy [--publish] [--force] [--js] [--pause]
      wrt --link [--noprompt] [--pause]
      wrt --clean [--pause]
```

### Description
The Web Resources Tool, wrt,  uploads WebResources into Dynamics CRM Organization using relative pathing and a directory structure. It calculates file hashes from WebResources in the Organization and on disk to determine the detal before uploading. This tool can help speed up any WebResource intensive CRM Organization with the use of just a few commands.

### Options
```
      --publish               When used with -d or --deploy, it will publish
                              deployed resources.
      --deploy                Deploy Package.
      --link                  Links existing records within CRM to current
                              package, also presents the option to link to a
                              particular solution.
      --noprompt              Does not prompt while linking packages.
      --force                 Forces a complete redeployment and or publish of
                              entire package.
      --create                Create Package and Exit. Use this to start a new
                              solution.
      --clean                 Deletes all old files from CRM system, or 
                              attempts to, and removes references once deleted.
      --js                    Only Deploys JavaScript & Mapping Files
      --pause                 Pauses output when finished.
      --conn                  Manages Connections.
  -h, --help                  Show this message and exit.
```

# License
The main body of work is MIT licensed. Any additonal libraries and tools may have different licenses that apply. Please reference the LICENSE file for each separate folder if present.
