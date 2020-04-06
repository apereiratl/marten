CREATE OR REPLACE FUNCTION {databaseSchema}.mt_immutable_Timestamp(value text) RETURNS timestamp with time zone LANGUAGE sql IMMUTABLE AS $function$
    select value::Timestamp
$function$;