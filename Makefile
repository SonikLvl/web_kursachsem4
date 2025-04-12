#Project Variables 

PROJECT_NAME ?= web_kursachsem4 

.PHONY: migrations db hello 

migrations: 
	dotnet ef --startup-project .\web_kursachsem4.Web\web_kursachsem4.Web.csproj --project .\web_kursachsem4.Data\web_kursachsem4.Data.csproj migrations add $(mname)
db: 
	dotnet ef --startup-project .\web_kursachsem4.Web\web_kursachsem4.Web.csproj --project .\web_kursachsem4.Data\web_kursachsem4.Data.csproj database update

hello: 
	echo 'hello!'