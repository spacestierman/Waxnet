Download & install Node.js for windows.

Globally install some NPM modules so that they can be called via command line from any directory.
> npm install browserify -g
> npm install uglify-js@1 -g
> npm install coffeeify -g
> npm install watchify -g

The `-g` installs modules to your local user AppData folder, and also creates some helpful .cmd files that can be called via command-line.
C:\Users\jonathon.stierman\AppData\Roaming\npm

Update your `Path` environment variable to include these .cmd helper directories so that you can call these modules via command-line 
%USERPROFILE%\AppData\Roaming\npm;%USERPROFILE%\AppData\Roaming\npm\node_modules\coffeeify\node_modules\.bin

Create a new System Variable `NODE_PATH`:
%USERPROFILE%\AppData\Roaming\npm\node_modules

# Setup site
Install IIS through Add/Remove Windows Features
Set up a new IIS site for your install
	example: gemini-wax.localhost
	Set up Virtual Directories to point to corresponding git directories (equivalent to symlinking)
		Ajax
		public
	Remember to update your HOSTS file

# Setup your project files
> cd D:\dev
> git clone git@github.space150.com:space150/gemini.git
> cd gemini
> npm install

browserify --extension=".coffee" -t coffeeify -t stripify -t uglifyify public/coffee/application.coffee -o public/javascripts/application.js
watchify --extension=".coffee" -t coffeeify public/coffee/application.coffee -dvo public/javascripts/application.js