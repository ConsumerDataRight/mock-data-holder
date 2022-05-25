openssl ecparam -name prime256v1 -genkey -noout -out es256-key.pem
openssl req -new -x509 -key es256-key.pem -out es256-public.pem -days 900000 -subj "/C=AU/ST=ACT/L=Canberra/O=ACCC/OU=CDR/CN=mdh-es256"

openssl ecparam -in es256-key.pem -text -noout
openssl x509 -in es256-public.pem -text -noout

get-content es256-key.pem,es256-public.pem | out-file es256-private.pem

openssl pkcs12 -export -inkey  es256-key.pem  -in es256-private.pem -out es256-private.pfx
