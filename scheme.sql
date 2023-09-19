--
-- PostgreSQL database dump
--

-- Dumped from database version 15.4 (Ubuntu 15.4-1.pgdg22.04+1)
-- Dumped by pg_dump version 15.4 (Ubuntu 15.4-1.pgdg22.04+1)

-- Started on 2023-09-19 15:17:36 UTC

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 214 (class 1259 OID 20139)
-- Name: badwords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.badwords (
    badword character varying
);


--
-- TOC entry 215 (class 1259 OID 20144)
-- Name: banreasons; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.banreasons (
    reason text,
    custom_id character varying
);


--
-- TOC entry 216 (class 1259 OID 20149)
-- Name: birthdays; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.birthdays (
    user_id bigint,
    datum character varying,
    ping boolean
);


--
-- TOC entry 217 (class 1259 OID 20154)
-- Name: countcounter; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.countcounter (
    userid bigint,
    counter bigint,
    timestamps bigint
);


--
-- TOC entry 218 (class 1259 OID 20157)
-- Name: counting; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.counting (
    lastnumber bigint,
    lastuser bigint
);


--
-- TOC entry 219 (class 1259 OID 20160)
-- Name: countingfails; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.countingfails (
    userid bigint,
    counter bigint
);


--
-- TOC entry 220 (class 1259 OID 20163)
-- Name: countinghighscore; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.countinghighscore (
    number bigint,
    userid bigint,
    timestamps bigint
);


--
-- TOC entry 221 (class 1259 OID 20166)
-- Name: countsave; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.countsave (
    userid bigint,
    saves numeric
);


--
-- TOC entry 222 (class 1259 OID 20171)
-- Name: datekicksettings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.datekicksettings (
    status boolean,
    days integer
);


--
-- TOC entry 223 (class 1259 OID 20174)
-- Name: flags; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.flags (
    userid bigint,
    punisherid bigint,
    datum bigint,
    description character varying,
    caseid character varying
);


--
-- TOC entry 224 (class 1259 OID 20179)
-- Name: mainchatautopost; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.mainchatautopost (
    usersjoined integer,
    messagessent integer
);


--
-- TOC entry 225 (class 1259 OID 20182)
-- Name: metrics; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.metrics (
    metrics character varying,
    "values" character varying,
    "time" bigint
);


--
-- TOC entry 226 (class 1259 OID 20187)
-- Name: tempvoice; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tempvoice (
    channelid bigint,
    ownerid bigint,
    lastedited bigint,
    channelmods character varying
);


--
-- TOC entry 227 (class 1259 OID 20192)
-- Name: tempvoicesession; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tempvoicesession (
    userid bigint,
    channelname character varying,
    channelbitrate integer,
    channellimit integer,
    blockedusers character varying,
    permitedusers character varying,
    locked boolean,
    hidden boolean
);


--
-- TOC entry 228 (class 1259 OID 20197)
-- Name: vorstellungscooldown; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.vorstellungscooldown (
    user_id bigint,
    "time" bigint
);


--
-- TOC entry 229 (class 1259 OID 20200)
-- Name: voting; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.voting (
    userid bigint,
    timestamps bigint,
    weekend boolean
);


--
-- TOC entry 230 (class 1259 OID 20203)
-- Name: warnreasons; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.warnreasons (
    reason text,
    custom_id character varying
);


--
-- TOC entry 231 (class 1259 OID 20208)
-- Name: warns; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.warns (
    userid bigint,
    punisherid bigint,
    datum bigint,
    description character varying,
    perma boolean,
    caseid character varying
);


-- Completed on 2023-09-19 15:17:36 UTC

--
-- PostgreSQL database dump complete
--

