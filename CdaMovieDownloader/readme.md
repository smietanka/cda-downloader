run postgres
`docker run --name postgres -e POSTGRES_PASSWORD=admin -e POSTGRES_USER=admin -d postgres`

run pgadmin

`docker run --name pgadmin-container -p 5050:80 -e PGADMIN_DEFAULT_EMAIL=admin@admin.com -e PGADMIN_DEFAULT_PASSWORD=admin -d dpage/pgadmin4`