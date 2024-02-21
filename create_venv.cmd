@echo off
setlocal

::download the python zip file latest from the python official site
powershell -Command "Invoke-WebRequest https://www.python.org/ftp/python/3.12.2/python-3.12.2-embed-amd64.zip -OutFile python.zip"

::unzip the python zip file
powershell -command "Expand-Archive -Force '%~dp0python.zip' '%~dp0python'"

::delete the python.zip file
del python.zip 2>nul

::download pip from official site and keep inside unzipped python folder
powershell -Command "Invoke-WebRequest https://bootstrap.pypa.io/get-pip.py -OutFile python/get-pip.py"

::search for a file ending with ._pth and add the site package location for running pip and -m commands
powershell -Command "Get-ChildItem -Path .\python -Filter '*._pth' -Recurse | ForEach-Object { Add-Content -Path $_.FullName -Value 'lib/site-packages' }"

::install pip
"python/python" "./python/get-pip.py"
"python/python" -m pip install virtualenv

::create virtual environment
"python/python" -m virtualenv venv

::remove python extracted folder, as it is not required after creating virtual environement
::powershell -command "Remove-Item -Path ./python -Recurse -Force"
rd /s /q python

::install the package dependencies
"venv/scripts/python" -m pip install -r requirements.txt