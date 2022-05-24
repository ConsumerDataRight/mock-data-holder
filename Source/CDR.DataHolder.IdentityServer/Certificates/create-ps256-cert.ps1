openssl genrsa -out ps256-key.pem 3072
openssl req -new -x509 -key ps256-key.pem -out ps256-public.pem -days 900000 -subj "/C=AU/ST=ACT/L=Canberra/O=ACCC/OU=CDR/CN=mdh-ps256"
openssl x509 -in ps256-public.pem -text -noout

get-content ps256-key.pem,ps256-public.pem | out-file ps256-private.pem

openssl pkcs12 -export -inkey  ps256-key.pem  -in ps256-private.pem -out ps256-private.pfx
